using System.Text.RegularExpressions;
using DiarioX.Server.Application.DTOs.Professores;
using DiarioX.Server.Application.DTOs.Users;
using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace DiarioX.Server.Application.Services;

public class ProfessorService : IProfessorService
{
    private static readonly Regex NonDigits = new(@"\D", RegexOptions.Compiled);
    private static readonly Regex EmailPattern = new(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.Compiled);

    private static readonly Dictionary<string, string> SituacoesValidas = new()
    {
        [Professor.StatusAtivo] = "Ativo",
        [Professor.StatusInativo] = "Inativo",
        [Professor.StatusAfastado] = "Afastado",
        [Professor.StatusLicenciado] = "Licenciado",
    };

    private readonly IProfessorRepository _professorRepository;
    private readonly IDisciplinaRepository _disciplinaRepository;
    private readonly IEscolaRepository _escolaRepository;
    private readonly IUserService _userService;
    private readonly IEmailNotificationService _emailNotificationService;
    private readonly ILogger<ProfessorService> _logger;

    public ProfessorService(
        IProfessorRepository professorRepository,
        IDisciplinaRepository disciplinaRepository,
        IEscolaRepository escolaRepository,
        IUserService userService,
        IEmailNotificationService emailNotificationService,
        ILogger<ProfessorService> logger)
    {
        _professorRepository = professorRepository;
        _disciplinaRepository = disciplinaRepository;
        _escolaRepository = escolaRepository;
        _userService = userService;
        _emailNotificationService = emailNotificationService;
        _logger = logger;
    }

    public async Task<ProfessorCommandResult> GetByIdAsync(int id)
    {
        var professor = await _professorRepository.GetByIdAsync(id);
        if (professor is null)
            return NotFound("Professor não encontrado.");

        return new ProfessorCommandResult(true, string.Empty, MapToResponse(professor));
    }

    public async Task<IEnumerable<ProfessorResponse>> GetAllAsync()
    {
        var professors = await _professorRepository.GetAllAsync();
        return professors.Select(MapToResponse);
    }

    public async Task<IEnumerable<ProfessorResponse>> GetByEscolaIdAsync(int escolaId)
    {
        var professors = await _professorRepository.GetByEscolaIdAsync(escolaId);
        return professors.Select(MapToResponse);
    }

    public async Task<ProfessorCommandResult> CreateAsync(ProfessorRequest request)
    {
        var normalized = NormalizeRequest(request);
        
        // Validar campos básicos
        var validation = ValidateCommonFields(normalized);
        if (!validation.Success)
            return validation;

        // Validar para criação
        var createValidation = await ValidateForCreateAsync(normalized);
        if (!createValidation.Success)
            return createValidation;

        try
        {
            // Criar entidade
            var professor = new Professor
            {
                Nome = normalized.Nome,
                Cpf = normalized.Cpf,
                DataNascimento = normalized.DataNascimento,
                Email = normalized.Email,
                Telefone = normalized.Telefone,
                Matricula = normalized.Matricula,
                DataAdmissao = normalized.DataAdmissao,
                Situacao = normalized.Situacao,
                EscolaId = normalized.EscolaId,
                CreatedAt = DateTime.UtcNow,
            };

            // Salvar professor
            var created = await _professorRepository.AddAsync(professor);

            // Adicionar relacionamentos com disciplinas
            foreach (var disciplinaId in normalized.DisciplinaIds)
            {
                await _professorRepository.AddDisciplinaAsync(created.Id, disciplinaId);
            }

            // Criar usuário automaticamente
            try
            {
                var userRequest = new UserRequest
                {
                    Email = created.Email,
                    Cpf = created.Cpf,
                    DataNascimento = created.DataNascimento,
                    Status = "ATIVO",
                    PerfilId = await GetProfessorPerfilIdAsync()
                };

                var userResult = await _userService.CreateAsync(userRequest);
                if (userResult.Success && userResult.User is not null)
                {
                    created.UsuarioId = userResult.User.Id;
                    await _professorRepository.UpdateAsync(created);
                    
                    _logger.LogInformation("Usuário criado automaticamente para professor {ProfessorId}", created.Id);
                }
                else
                {
                    _logger.LogWarning("Falha ao criar usuário para professor {ProfessorId}: {Message}", 
                        created.Id, userResult.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar usuário para professor {ProfessorId}", created.Id);
            }

            // Recarregar com relacionamentos
            var createdWithNav = await _professorRepository.GetByIdAsync(created.Id);
            if (createdWithNav is null)
                return new ProfessorCommandResult(false, "Erro ao recarregar professor criado.");

            // Enviar email de boas-vindas específico para professor
            _ = SendWelcomeProfessorEmailAsync(createdWithNav);

            return new ProfessorCommandResult(
                true,
                "Professor cadastrado com sucesso!",
                MapToResponse(createdWithNav));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar professor");
            return Invalid("Erro ao salvar professor. Tente novamente.");
        }
    }

    public async Task<ProfessorCommandResult> UpdateAsync(int id, ProfessorRequest request)
    {
        var professor = await _professorRepository.GetByIdAsync(id);
        if (professor is null)
            return NotFound("Professor não encontrado.");

        var normalized = NormalizeRequest(request);

        // Validar campos básicos
        var validation = ValidateCommonFields(normalized);
        if (!validation.Success)
            return validation;

        // Validar para atualização
        var updateValidation = await ValidateForUpdateAsync(id, normalized);
        if (!updateValidation.Success)
            return updateValidation;

        try
        {
            // Atualizar dados
            professor.Nome = normalized.Nome;
            professor.Cpf = normalized.Cpf;
            professor.DataNascimento = normalized.DataNascimento;
            professor.Email = normalized.Email;
            professor.Telefone = normalized.Telefone;
            professor.Matricula = normalized.Matricula;
            professor.DataAdmissao = normalized.DataAdmissao;
            professor.Situacao = normalized.Situacao;
            professor.EscolaId = normalized.EscolaId;
            professor.UpdatedAt = DateTime.UtcNow;

            // Remover disciplinas antigas e adicionar novas
            await _professorRepository.RemoveDisciplinasAsync(professor.Id);

            foreach (var disciplinaId in normalized.DisciplinaIds)
            {
                await _professorRepository.AddDisciplinaAsync(professor.Id, disciplinaId);
            }

            await _professorRepository.UpdateAsync(professor);

            var updated = await _professorRepository.GetByIdAsync(id);
            if (updated is null)
                return new ProfessorCommandResult(false, "Erro ao recarregar professor atualizado.");

            return new ProfessorCommandResult(
                true,
                "Professor atualizado com sucesso!",
                MapToResponse(updated));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar professor {Id}", id);
            return Invalid("Erro ao atualizar professor. Tente novamente.");
        }
    }

    public async Task<ProfessorCommandResult> DeleteAsync(int id)
    {
        var professor = await _professorRepository.GetByIdAsync(id);
        if (professor is null)
            return NotFound("Professor não encontrado.");

        try
        {
            await _professorRepository.DeleteAsync(professor);
            _logger.LogInformation("Professor {Id} deletado com sucesso", id);

            return new ProfessorCommandResult(true, "Professor removido com sucesso!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao deletar professor {Id}", id);
            return Invalid("Erro ao remover professor. Tente novamente.");
        }
    }

    private static ProfessorRequest NormalizeRequest(ProfessorRequest request)
    {
        return new ProfessorRequest
        {
            Nome = (request.Nome ?? string.Empty).Trim(),
            Cpf = NonDigits.Replace((request.Cpf ?? string.Empty).Trim(), string.Empty),
            DataNascimento = request.DataNascimento,
            Email = (request.Email ?? string.Empty).Trim().ToLowerInvariant(),
            Telefone = (request.Telefone ?? string.Empty).Trim(),
            Matricula = (request.Matricula ?? string.Empty).Trim(),
            DataAdmissao = request.DataAdmissao,
            Situacao = (request.Situacao ?? "ATIVO").Trim().ToUpperInvariant(),
            EscolaId = request.EscolaId,
            DisciplinaIds = request.DisciplinaIds ?? new List<int>(),
        };
    }

    private static ProfessorCommandResult ValidateCommonFields(ProfessorRequest request)
    {
        // RN03 - Validar campos obrigatórios
        if (string.IsNullOrWhiteSpace(request.Nome))
            return Invalid("Nome do professor é obrigatório.");

        if (string.IsNullOrWhiteSpace(request.Cpf) || request.Cpf.Length < 11)
            return Invalid("CPF inválido (informe 11 dígitos válidos).");

        if (!IsValidCpf(request.Cpf))
            return Invalid("CPF inválido (informe 11 dígitos válidos).");

        if (request.DataNascimento == default)
            return Invalid("Data de nascimento é obrigatória.");

        // EX03 - Validar email
        if (string.IsNullOrWhiteSpace(request.Email))
            return Invalid("Email é obrigatório.");

        if (!EmailPattern.IsMatch(request.Email))
            return Invalid("Por favor, insira um endereço de e-mail válido.");

        if (string.IsNullOrWhiteSpace(request.Telefone))
            return Invalid("Telefone é obrigatório.");

        if (string.IsNullOrWhiteSpace(request.Matricula))
            return Invalid("Matrícula é obrigatória.");

        if (request.DataAdmissao == default)
            return Invalid("Data de admissão é obrigatória.");

        if (request.EscolaId <= 0)
            return Invalid("Escola é obrigatória.");

        // EX01 - Validar disciplinas
        if (request.DisciplinaIds is null || request.DisciplinaIds.Count == 0)
            return Invalid("Por favor, selecione pelo menos uma disciplina.");

        // Validar situação
        if (!SituacoesValidas.ContainsKey(request.Situacao))
            return Invalid("Situação inválida. Opções válidas: Ativo, Inativo, Afastado e Licenciado.");

        return new ProfessorCommandResult(true, string.Empty);
    }

    private async Task<ProfessorCommandResult> ValidateForCreateAsync(ProfessorRequest request)
    {
        // RN01 - Validar duplicação de CPF
        if (await _professorRepository.ExistsByCpfAsync(request.Cpf))
            return Conflict("Este CPF já está vinculado a um professor cadastrado.");

        // RN01 - Validar duplicação de Matrícula
        if (await _professorRepository.ExistsByMatriculaAsync(request.Matricula))
            return Conflict("Esta matrícula já está vinculada a um professor cadastrado.");

        // Validar duplicação de Email
        if (await _professorRepository.ExistsByEmailAsync(request.Email))
            return Conflict("Este email já está vinculado a um professor cadastrado.");

        // Validar escola
        var escola = await _escolaRepository.GetByIdAsync(request.EscolaId);
        if (escola is null)
            return DependencyNotFound("Escola não encontrada.");

        // Validar disciplinas
        var disciplinas = new List<Disciplina>();
        foreach (var disciplinaId in request.DisciplinaIds)
        {
            var disciplina = await _disciplinaRepository.GetByIdAsync(disciplinaId);
            if (disciplina is null)
                return DependencyNotFound($"Disciplina com ID {disciplinaId} não encontrada.");

            disciplinas.Add(disciplina);
        }

        // Validar unicidade de disciplinas na lista
        if (request.DisciplinaIds.Count != request.DisciplinaIds.Distinct().Count())
            return Invalid("Existem disciplinas duplicadas na lista.");

        return new ProfessorCommandResult(true, string.Empty);
    }

    private async Task<ProfessorCommandResult> ValidateForUpdateAsync(int id, ProfessorRequest request)
    {
        // RN01 - Validar CPF (excluindo o próprio)
        var cpfExists = await _professorRepository.ExistsByCpfAsync(request.Cpf);
        if (cpfExists)
        {
            var existing = await _professorRepository.GetByCpfAsync(request.Cpf);
            if (existing?.Id != id)
                return Conflict("Este CPF já está vinculado a outro professor.");
        }

        // RN01 - Validar Matrícula (excluindo o próprio)
        var matriculaExists = await _professorRepository.ExistsByMatriculaAsync(request.Matricula);
        if (matriculaExists)
        {
            var existing = await _professorRepository.GetByMatriculaAsync(request.Matricula);
            if (existing?.Id != id)
                return Conflict("Esta matrícula já está vinculada a outro professor.");
        }

        // Validar Email (excluindo o próprio)
        var emailExists = await _professorRepository.ExistsByEmailAsync(request.Email);
        if (emailExists)
        {
            var professor = await _professorRepository.GetByIdAsync(id);
            if (professor?.Email != request.Email)
                return Conflict("Este email já está vinculado a outro professor.");
        }

        // Validar escola
        var escola = await _escolaRepository.GetByIdAsync(request.EscolaId);
        if (escola is null)
            return DependencyNotFound("Escola não encontrada.");

        // Validar disciplinas
        foreach (var disciplinaId in request.DisciplinaIds)
        {
            var disciplina = await _disciplinaRepository.GetByIdAsync(disciplinaId);
            if (disciplina is null)
                return DependencyNotFound($"Disciplina com ID {disciplinaId} não encontrada.");
        }

        // Validar unicidade de disciplinas na lista
        if (request.DisciplinaIds.Count != request.DisciplinaIds.Distinct().Count())
            return Invalid("Existem disciplinas duplicadas na lista.");

        return new ProfessorCommandResult(true, string.Empty);
    }

    private static bool IsValidCpf(string cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf) || cpf.Length != 11)
            return false;

        if (!cpf.All(char.IsDigit))
            return false;

        // Rejeitar CPF com todos os dígitos iguais
        if (cpf == new string(cpf[0], 11))
            return false;

        // Validar dígito verificador
        int CalculateDigit(string value, int[] weights)
        {
            int sum = value.Select((c, i) => (c - '0') * weights[i]).Sum();
            int remainder = sum % 11;
            return remainder < 2 ? 0 : 11 - remainder;
        }

        int[] weights1 = { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
        int[] weights2 = { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };

        int digit1 = CalculateDigit(cpf[..9], weights1);
        int digit2 = CalculateDigit(cpf[..10], weights2);

        return (cpf[9] - '0' == digit1) && (cpf[10] - '0' == digit2);
    }

    private async Task<int?> GetProfessorPerfilIdAsync()
    {
        // TODO: Implementar busca de Perfil "Professor" no banco
        // Por enquanto, retornar null e deixar sem perfil
        return null;
    }

    private static ProfessorCommandResult Invalid(string message)
        => new(false, message, Error: ProfessorResultError.Validation);

    private static ProfessorCommandResult NotFound(string message)
        => new(false, message, Error: ProfessorResultError.NotFound);

    private static ProfessorCommandResult Conflict(string message)
        => new(false, message, Error: ProfessorResultError.Conflict);

    private static ProfessorCommandResult DependencyNotFound(string message)
        => new(false, message, Error: ProfessorResultError.DependencyNotFound);

    private static ProfessorResponse MapToResponse(Professor professor)
    {
        var disciplinas = professor.ProfessorDisciplinas
            .Select(pd => new DisciplinaResponseForProfessor(pd.Disciplina.Id, pd.Disciplina.Nome))
            .ToList();

        return new ProfessorResponse(
            professor.Id,
            professor.Nome,
            FormatCpf(professor.Cpf),
            professor.DataNascimento,
            professor.Email,
            professor.Telefone,
            professor.Matricula,
            professor.DataAdmissao,
            professor.Situacao,
            professor.EscolaId,
            professor.Escola.Nome,
            disciplinas,
            professor.UsuarioId,
            professor.Usuario?.Email,
            professor.CreatedAt,
            professor.UpdatedAt
        );
    }

    private static string FormatCpf(string cpf)
    {
        if (cpf.Length != 11)
            return cpf;

        return $"{cpf[0..3]}.{cpf[3..6]}.{cpf[6..9]}-{cpf[9..11]}";
    }

    /// <summary>
    /// Envia email de boas-vindas específico para professor.
    /// Executado de forma não-bloqueante para não impactar a criação do professor.
    /// </summary>
    private async Task SendWelcomeProfessorEmailAsync(Professor professor)
    {
        try
        {
            await _emailNotificationService.SendWelcomeProfessorAsync(
                professor.Email,
                professor.Nome,
                professor.Escola.Nome,
                professor.Email);

            _logger.LogInformation("Email de boas-vindas de professor enviado com sucesso para {Email}", professor.Email);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao enviar email de boas-vindas de professor para {Email}. O professor foi criado normalmente.", professor.Email);
            // Não relançar exceção - criação do professor já foi bem-sucedida
        }
    }
}
