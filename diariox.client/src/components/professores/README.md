# 📚 Módulo de Professor - Frontend

## Visão Geral

Componente React/TypeScript para gerenciar o cadastro de professores no sistema DiarioX.

## Localização dos Arquivos

- **Componente**: `src/components/professores/ProfessoresPage.tsx`
- **Estilos**: `src/components/MainContent.css`
- **Navegação**: `src/components/Sidebar.tsx` (atualizado)
- **Roteamento**: `src/components/MainContent.tsx` (atualizado)

## Funcionalidades

### 1. Listar Professores
- ✅ Exibe todos os professores cadastrados em uma tabela
- ✅ Mostra: Nome, CPF, Matrícula, Email, Disciplinas, Situação
- ✅ Ações: Editar, Deletar
- ✅ Empty state quando nenhum professor existe

### 2. Cadastrar Novo Professor
- ✅ Formulário dividido em 3 seções:
  - **Dados Pessoais**: Nome, CPF, Data de Nascimento, Email, Telefone
  - **Dados Contratuais**: Matrícula, Data de Admissão, Situação, Escola
  - **Habilitação Pedagógica**: Seleção de disciplinas (multi-select)

### 3. Editar Professor
- ✅ Carrega dados existentes no formulário
- ✅ Atualiza o registro no banco de dados
- ✅ Volta à listagem após salvar

### 4. Deletar Professor
- ✅ Confirmação antes de deletar
- ✅ Remove registro permanentemente
- ✅ Atualiza listagem automaticamente

## Validações Implementadas

### Frontend
- ✅ Campos obrigatórios destacados com `*`
- ✅ CPF: 11 dígitos (máximo 14 com máscara)
- ✅ Email: formato válido (ex: usuario@email.com)
- ✅ Telefone: 11 dígitos (máximo 15 com máscara)
- ✅ Mínimo 1 disciplina selecionada
- ✅ Validação em tempo real

### Backend
- ✅ Validação de CPF com algoritmo
- ✅ Email único
- ✅ Matrícula única
- ✅ Escola existe
- ✅ Disciplinas existem
- ✅ Sem disciplinas duplicadas

## Máscaras de Entrada

| Campo | Máscara | Exemplo |
|-------|---------|---------|
| CPF | 000.000.000-00 | 123.456.789-00 |
| Telefone | (00) 00000-0000 | (11) 98765-4321 |

## Fluxo de Uso

### 1. Acessar o módulo
```
1. Clicar em "Cadastro" no menu lateral
2. Selecionar "👨‍🏫 Professores"
```

### 2. Listar professores
```
A página carrega automaticamente com:
- Todos os professores cadastrados
- Botão "➕ Novo Professor"
- Tabela com ações (Editar, Deletar)
```

### 3. Criar novo professor
```
1. Clicar "➕ Novo Professor"
2. Preencher formulário (3 seções):
   - Dados Pessoais
   - Dados Contratuais
   - Habilitação Pedagógica
3. Clicar "Salvar"
4. Confirmação: "Professor cadastrado com sucesso!"
5. Voltar automaticamente à listagem
```

### 4. Editar professor
```
1. Clicar "✏️ Editar" na tabela
2. Formulário pré-preenchido
3. Modificar dados
4. Clicar "Atualizar"
5. Confirmação: "Professor atualizado com sucesso!"
```

### 5. Deletar professor
```
1. Clicar "🗑️ Deletar" na tabela
2. Confirmação: "Tem certeza que deseja remover..."
3. Clicar "OK"
4. Confirmação: "Professor removido com sucesso!"
```

## Estados e Feedback

### Mensagens de Sucesso
- ✅ "Professor cadastrado com sucesso!" (criação)
- ✅ "Professor atualizado com sucesso!" (edição)
- ✅ "Professor removido com sucesso!" (deleção)

### Mensagens de Erro
- ❌ "Por favor, preencha todos os campos obrigatórios."
- ❌ "CPF inválido (11 dígitos)"
- ❌ "Email inválido (ex: usuario@email.com)"
- ❌ "Selecione pelo menos uma disciplina"
- ❌ "Este CPF já está vinculado a um professor cadastrado."
- ❌ "Esta matrícula já está vinculada a um professor cadastrado."
- ❌ "Este email já está vinculado a um professor cadastrado."

### Estados de Carregamento
- ⏳ "Carregando professores..."
- ⏳ Botões desabilitados durante operações
- ⏳ "Salvando..." ao submeter formulário

## API Integration

### Endpoints Utilizados

```
GET    /api/professores                 # Listar todos
GET    /api/professores/{id}            # Obter por ID
GET    /api/professores/escola/{id}     # Listar por escola
POST   /api/professores                 # Criar
PUT    /api/professores/{id}            # Atualizar
DELETE /api/professores/{id}            # Deletar

GET    /api/escolas                     # Carregar opções de escola
GET    /api/disciplinas                 # Carregar opções de disciplinas
```

### Request/Response

**POST/PUT** `/api/professores`
```json
{
    "nome": "João Silva",
    "cpf": "12345678901",
    "dataNascimento": "1985-05-15",
    "email": "joao@escola.com",
    "telefone": "(11) 98765-4321",
    "matricula": "MAT-001",
    "dataAdmissao": "2020-01-15",
    "situacao": "ATIVO",
    "escolaId": 1,
    "disciplinaIds": [1, 2, 3]
}
```

**GET** `/api/professores/{id}`
```json
{
    "id": 1,
    "nome": "João Silva",
    "cpf": "123.456.789-00",
    "dataNascimento": "1985-05-15",
    "email": "joao@escola.com",
    "telefone": "(11) 98765-4321",
    "matricula": "MAT-001",
    "dataAdmissao": "2020-01-15",
    "situacao": "ATIVO",
    "escolaId": 1,
    "escolaNome": "Escola A",
    "disciplinas": [
        { "id": 1, "nome": "Matemática" },
        { "id": 2, "nome": "Física" }
    ],
    "usuarioId": 1,
    "usuarioEmail": "123.456.789-00@diariox.local",
    "createdAt": "2026-09-20T10:00:00Z",
    "updatedAt": null
}
```

## Componentes e Hooks Utilizados

### Hooks Customizados
- `useCrudData` - Gerencia fetch, save, remove com estado
- `useState` - Gerencia formulário, erros, mensagens
- `useEffect` - Carrega dados na inicialização

### Componentes UI Reutilizáveis
- `FeedbackMessage` - Exibe mensagens de erro/sucesso
- `EmptyState` - Mostra estado vazio da tabela
- `StatusPill` - Badge com situação do professor

## Responsividade

✅ Desktop (1024px+)
- Tabela completa
- Formulário em 2 colunas
- Checkboxes em grid

✅ Tablet (768px - 1024px)
- Tabela scrollável
- Formulário em 1 coluna
- Menu reduzido

✅ Mobile (< 768px)
- Tabela comprimida
- Formulário fullwidth
- Botões stacked

## Performance

- ✅ Lazy loading de opções (Escolas, Disciplinas)
- ✅ Reutilização de hooks
- ✅ Sem re-renders desnecessários
- ✅ Validação síncrona

## Testes Manuais

### Criar professor válido
```
1. Abrir módulo de Professores
2. Clicar "Novo Professor"
3. Preencher:
   - Nome: João Silva
   - CPF: 12345678901
   - Data: 1985-05-15
   - Email: joao@escola.com
   - Telefone: 11987654321
   - Matrícula: MAT-001
   - Data Admissão: 2020-01-15
   - Situação: ATIVO
   - Escola: Selecionar uma
   - Disciplinas: Selecionar mínimo 1
4. Clicar "Salvar"
5. Esperado: "Professor cadastrado com sucesso!" ✅
```

### Validar CPF inválido
```
1. Preencher CPF com "00000000000"
2. Deixar em branco
3. Clicar "Salvar"
4. Esperado: Erro "CPF inválido (11 dígitos)" ✅
```

### Validar email inválido
```
1. Preencher Email: "email_sem_arroba"
2. Clicar "Salvar"
3. Esperado: Erro "Email inválido (ex: usuario@email.com)" ✅
```

### Validar duplicação de CPF
```
1. Criar 2 professores com mesmo CPF
2. Clicar "Salvar" no segundo
3. Esperado: Erro 409 "Este CPF já está vinculado..." ✅
```

## Estrutura de Código

```
ProfessoresPage.tsx
├── Estados
│   ├── view (list/form)
│   ├── form (dados do formulário)
│   ├── fieldErrors (erros por campo)
│   ├── professores (listagem)
│   ├── escolas (opções)
│   └── disciplinas (opções)
├── Efeitos
│   ├── load() - Carrega professores
│   └── loadOptions() - Carrega escolas e disciplinas
├── Handlers
│   ├── handleFieldChange() - Atualiza campo
│   ├── handleFormSubmit() - Salva professor
│   ├── handleEditClick() - Inicia edição
│   ├── handleDeleteClick() - Remove professor
│   └── handleNewProfessor() - Novo formulário
└── Render
    ├── View: list
    │   ├── Botão "Novo"
    │   ├── Tabela com professores
    │   └── Ações (Editar, Deletar)
    └── View: form
        ├── 3 fieldsets (Pessoais, Contratuais, Pedagógicas)
        └── Buttons (Salvar, Cancelar)
```

## Próximas Melhorias (Opcional)

- [ ] Filtro por escola na listagem
- [ ] Filtro por situação (ATIVO/INATIVO)
- [ ] Busca por nome/CPF/Matrícula
- [ ] Paginação na tabela
- [ ] Exportar em CSV/PDF
- [ ] Upload de foto
- [ ] Histórico de alterações
- [ ] Confirmação antes de sair do formulário

## Suporte

Para relatórios de bugs ou sugestões, contate a equipe de desenvolvimento.

---

**Status**: ✅ Production Ready
**Última atualização**: 2026-09-20
**Versão**: 1.0.0
