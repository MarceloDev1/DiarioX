using System.Text.RegularExpressions;
using DiarioX.Server.Application.DTOs.Alunos;
using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;

namespace DiarioX.Server.Application.Services;

public class AlunoService : IAlunoService
{
    private static readonly Regex NonDigits = new(@"\D", RegexOptions.Compiled);

    private readonly IAlunoRepository _alunoRepository;
    private readonly IEscolaRepository _escolaRepository;
    private readonly IAlunoTurmaRepository _alunoTurmaRepository;

    public AlunoService(IAlunoRepository alunoRepository, IEscolaRepository escolaRepository, IAlunoTurmaRepository alunoTurmaRepository)
    {
        _alunoRepository = alunoRepository;
        _escolaRepository = escolaRepository;
        _alunoTurmaRepository = alunoTurmaRepository;
    }

    public async Task<AlunoCommandResult> GetByIdAsync(int id)
    {
        var aluno = await _alunoRepository.GetByIdAsync(id);
        if (aluno is null)
            return NotFound("Aluno não encontrado.");

        return new AlunoCommandResult(true, string.Empty, MapToResponse(aluno));
    }

    public async Task<IEnumerable<AlunoResponse>> GetAllAsync()
    {
        var alunos = await _alunoRepository.GetAllAsync();
        return alunos.Select(MapToResponse);
    }

    public async Task<AlunoCommandResult> CreateAsync(AlunoRequest request)
    {
        var normalized = NormalizeRequest(request);
        var validation = await ValidateRequestAsync(normalized, null);
        if (!validation.Success)
            return validation;

        var matricula = await GerarMatriculaAsync();

        var aluno = new Aluno
        {
            Matricula = matricula,
            Nome = normalized.Nome,
            DataNascimento = normalized.DataNascimento,
            Sexo = normalized.Sexo,
            CorRaca = normalized.CorRaca,
            NecessidadeEspecial = normalized.NecessidadeEspecial,
            ResponsavelNome1 = normalized.ResponsavelNome1,
            ResponsavelCpf1 = normalized.ResponsavelCpf1,
            ResponsavelTelefone1 = normalized.ResponsavelTelefone1,
            ResponsavelNome2 = normalized.ResponsavelNome2,
            CpfAluno = normalized.CpfAluno,
            CertidaoNascimento = normalized.CertidaoNascimento,
            Cep = normalized.Cep,
            EnderecoCompleto = normalized.EnderecoCompleto,
            Numero = normalized.Numero,
            Bairro = normalized.Bairro,
            EscolaId = normalized.EscolaId,
            Status = Aluno.StatusAtivoAguardandoEnturmacao,
            CreatedAt = DateTime.UtcNow,
        };

        var created = await _alunoRepository.AddAsync(aluno);
        var reloaded = await _alunoRepository.GetByIdAsync(created.Id);
        return new AlunoCommandResult(true, "Aluno cadastrado com sucesso!", MapToResponse(reloaded!));
    }

    public async Task<AlunoCommandResult> UpdateAsync(int id, AlunoRequest request)
    {
        var aluno = await _alunoRepository.GetByIdAsync(id);
        if (aluno is null)
            return NotFound("Aluno não encontrado.");

        var normalized = NormalizeRequest(request);
        var validation = await ValidateRequestAsync(normalized, id);
        if (!validation.Success)
            return validation;

        aluno.Nome = normalized.Nome;
        aluno.DataNascimento = normalized.DataNascimento;
        aluno.Sexo = normalized.Sexo;
        aluno.CorRaca = normalized.CorRaca;
        aluno.NecessidadeEspecial = normalized.NecessidadeEspecial;
        aluno.ResponsavelNome1 = normalized.ResponsavelNome1;
        aluno.ResponsavelCpf1 = normalized.ResponsavelCpf1;
        aluno.ResponsavelTelefone1 = normalized.ResponsavelTelefone1;
        aluno.ResponsavelNome2 = normalized.ResponsavelNome2;
        aluno.CpfAluno = normalized.CpfAluno;
        aluno.CertidaoNascimento = normalized.CertidaoNascimento;
        aluno.Cep = normalized.Cep;
        aluno.EnderecoCompleto = normalized.EnderecoCompleto;
        aluno.Numero = normalized.Numero;
        aluno.Bairro = normalized.Bairro;
        aluno.EscolaId = normalized.EscolaId;
        aluno.UpdatedAt = DateTime.UtcNow;
        // RN02: Matricula (e Status) propositalmente não são reatribuídos aqui.

        await _alunoRepository.UpdateAsync(aluno);

        var updated = await _alunoRepository.GetByIdAsync(id);
        return new AlunoCommandResult(true, "Aluno atualizado com sucesso!", MapToResponse(updated!));
    }

    public async Task<AlunoCommandResult> UpdateStatusAsync(int id, AlunoStatusRequest request)
    {
        var status = (request.Status ?? string.Empty).Trim().ToUpperInvariant();
        if (status != Aluno.StatusAtivo && status != Aluno.StatusInativo)
            return Invalid("Status inválido. Valores permitidos: ATIVO ou INATIVO.");

        var aluno = await _alunoRepository.GetByIdAsync(id);
        if (aluno is null)
            return NotFound("Aluno não encontrado.");

        if (status == Aluno.StatusInativo)
        {
            aluno.Status = Aluno.StatusInativo;
        }
        else
        {
            // Ao reativar, o status volta a refletir a enturmação atual do aluno.
            var enturmacaoAtiva = await _alunoTurmaRepository.GetAtivaByAlunoIdAsync(id);
            aluno.Status = enturmacaoAtiva is null ? Aluno.StatusAtivoAguardandoEnturmacao : Aluno.StatusAtivo;
        }

        aluno.UpdatedAt = DateTime.UtcNow;
        await _alunoRepository.UpdateAsync(aluno);

        var message = status == Aluno.StatusInativo ? "Aluno inativado com sucesso!" : "Aluno ativado com sucesso!";
        return new AlunoCommandResult(true, message, MapToResponse(aluno));
    }

    public async Task<AlunoCommandResult> DeleteAsync(int id)
    {
        var aluno = await _alunoRepository.GetByIdAsync(id);
        if (aluno is null)
            return NotFound("Aluno não encontrado.");

        // O histórico de enturmação (atual ou passado) precisa ser preservado
        if (await _alunoTurmaRepository.ExistsByAlunoIdAsync(id))
            return Conflict("Este aluno possui histórico de enturmação e não pode ser excluído. Utilize a opção Inativar.");

        await _alunoRepository.DeleteAsync(aluno);
        return new AlunoCommandResult(true, "Aluno removido com sucesso!");
    }

    private async Task<string> GerarMatriculaAsync()
    {
        var ano = DateTime.UtcNow.Year;
        var sequencial = await _alunoRepository.GetMaxSequencialMatriculaAsync(ano) + 1;
        var matricula = $"{ano}{sequencial:D4}";

        // Defesa contra corrida rara entre o cálculo do próximo sequencial e a gravação.
        while (await _alunoRepository.ExistsByMatriculaAsync(matricula))
        {
            sequencial++;
            matricula = $"{ano}{sequencial:D4}";
        }

        return matricula;
    }

    private async Task<AlunoCommandResult> ValidateRequestAsync(AlunoRequest request, int? excludeId)
    {
        if (string.IsNullOrWhiteSpace(request.Nome))
            return Invalid("Nome completo é obrigatório.");

        if (request.DataNascimento == default || request.DataNascimento.Date > DateTime.UtcNow.Date)
            return Invalid("Data de nascimento é obrigatória.");

        if (request.Sexo != Aluno.SexoMasculino && request.Sexo != Aluno.SexoFeminino)
            return Invalid("Sexo/Gênero é obrigatório.");

        if (string.IsNullOrWhiteSpace(request.ResponsavelNome1))
            return Invalid("Nome do responsável legal é obrigatório.");

        if (string.IsNullOrWhiteSpace(request.ResponsavelCpf1))
            return Invalid("CPF do responsável é obrigatório.");

        if (!IsValidCpf(request.ResponsavelCpf1))
            return Invalid("CPF inválido.");

        if (string.IsNullOrWhiteSpace(request.ResponsavelTelefone1))
            return Invalid("Telefone de contato do responsável é obrigatório.");

        var possuiCpf = !string.IsNullOrWhiteSpace(request.CpfAluno);
        var idade = CalcularIdade(request.DataNascimento);

        // RN03 - aluno maior de idade exige CPF próprio.
        if (idade >= 18 && !possuiCpf)
            return Invalid("CPF do aluno é obrigatório para alunos maiores de 18 anos.");

        if (possuiCpf && !IsValidCpf(request.CpfAluno!))
            return Invalid("CPF inválido.");

        if (!possuiCpf && string.IsNullOrWhiteSpace(request.CertidaoNascimento))
            return Invalid("Certidão de nascimento é obrigatória quando o aluno não possui CPF.");

        if (string.IsNullOrWhiteSpace(request.Cep))
            return Invalid("CEP é obrigatório.");

        if (string.IsNullOrWhiteSpace(request.EnderecoCompleto))
            return Invalid("Endereço completo é obrigatório.");

        if (string.IsNullOrWhiteSpace(request.Numero))
            return Invalid("Número é obrigatório.");

        if (string.IsNullOrWhiteSpace(request.Bairro))
            return Invalid("Bairro é obrigatório.");

        if (request.EscolaId <= 0)
            return Invalid("Escola é obrigatória.");

        var escola = await _escolaRepository.GetByIdAsync(request.EscolaId);
        if (escola is null)
            return DependencyNotFound("Escola não encontrada.");

        // RN01 - chave de unicidade: CPF do aluno ou, na ausência dele, Nome + Nascimento + Responsável 1.
        var duplicado = possuiCpf
            ? await _alunoRepository.ExistsByCpfAsync(request.CpfAluno!, excludeId)
            : await _alunoRepository.ExistsByNomeDataNascimentoResponsavelAsync(request.Nome, request.DataNascimento, request.ResponsavelNome1, excludeId);

        if (duplicado)
            return Conflict("Atenção: Já existe um aluno cadastrado no sistema com estes dados.");

        return new AlunoCommandResult(true, string.Empty);
    }

    private static AlunoRequest NormalizeRequest(AlunoRequest request)
    {
        var cpfAluno = NonDigits.Replace(request.CpfAluno ?? string.Empty, string.Empty);

        return new AlunoRequest
        {
            Nome = (request.Nome ?? string.Empty).Trim(),
            DataNascimento = request.DataNascimento,
            Sexo = (request.Sexo ?? string.Empty).Trim().ToUpperInvariant(),
            CorRaca = string.IsNullOrWhiteSpace(request.CorRaca) ? null : request.CorRaca.Trim().ToUpperInvariant(),
            NecessidadeEspecial = request.NecessidadeEspecial,
            ResponsavelNome1 = (request.ResponsavelNome1 ?? string.Empty).Trim(),
            ResponsavelCpf1 = NonDigits.Replace(request.ResponsavelCpf1 ?? string.Empty, string.Empty),
            ResponsavelTelefone1 = NonDigits.Replace(request.ResponsavelTelefone1 ?? string.Empty, string.Empty),
            ResponsavelNome2 = string.IsNullOrWhiteSpace(request.ResponsavelNome2) ? null : request.ResponsavelNome2.Trim(),
            CpfAluno = cpfAluno.Length > 0 ? cpfAluno : null,
            CertidaoNascimento = string.IsNullOrWhiteSpace(request.CertidaoNascimento) ? null : request.CertidaoNascimento.Trim(),
            Cep = NonDigits.Replace(request.Cep ?? string.Empty, string.Empty),
            EnderecoCompleto = (request.EnderecoCompleto ?? string.Empty).Trim(),
            Numero = (request.Numero ?? string.Empty).Trim(),
            Bairro = (request.Bairro ?? string.Empty).Trim(),
            EscolaId = request.EscolaId,
        };
    }

    private static int CalcularIdade(DateTime dataNascimento)
    {
        var hoje = DateTime.UtcNow.Date;
        var idade = hoje.Year - dataNascimento.Year;
        if (dataNascimento.Date > hoje.AddYears(-idade))
            idade--;

        return idade;
    }

    private static bool IsValidCpf(string cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf) || cpf.Length != 11 || cpf.Distinct().Count() == 1)
            return false;

        var numbers = cpf.Select(c => c - '0').ToArray();
        var firstDigit = CalculateCpfDigit(numbers, 9, 10);
        if (numbers[9] != firstDigit) return false;

        var secondDigit = CalculateCpfDigit(numbers, 10, 11);
        return numbers[10] == secondDigit;
    }

    private static int CalculateCpfDigit(int[] numbers, int length, int weightStart)
    {
        var sum = 0;
        for (var i = 0; i < length; i++)
            sum += numbers[i] * (weightStart - i);

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }

    private static AlunoCommandResult Invalid(string message)
        => new(false, message, Error: AlunoResultError.Validation);

    private static AlunoCommandResult NotFound(string message)
        => new(false, message, Error: AlunoResultError.NotFound);

    private static AlunoCommandResult Conflict(string message)
        => new(false, message, Error: AlunoResultError.Conflict);

    private static AlunoCommandResult DependencyNotFound(string message)
        => new(false, message, Error: AlunoResultError.DependencyNotFound);

    private static AlunoResponse MapToResponse(Aluno aluno) => new(
        aluno.Id,
        aluno.Matricula,
        aluno.Nome,
        aluno.DataNascimento,
        aluno.Sexo,
        aluno.CorRaca,
        aluno.NecessidadeEspecial,
        aluno.CpfAluno,
        aluno.CertidaoNascimento,
        aluno.ResponsavelNome1,
        aluno.ResponsavelCpf1,
        aluno.ResponsavelTelefone1,
        aluno.ResponsavelNome2,
        aluno.Cep,
        aluno.EnderecoCompleto,
        aluno.Numero,
        aluno.Bairro,
        aluno.EscolaId,
        aluno.Escola.Nome,
        aluno.Status,
        aluno.CreatedAt,
        aluno.UpdatedAt
    );
}
