# Reborn — Protocolo de qualidade entre versões

## Uma versão por objetivo

Antes de criar uma nova funcionalidade, registrar o problema
reproduzível, a regra esperada, como será testado e qual versão será
preservada. Evitar misturar problemas de fusão, teclado, importação,
artes e IA num mesmo conjunto de alterações sem testes independentes.

## Ordem das verificações

**Gate 1 — Código**: confirmar diff, ausência de bytes da ISO/ROM
no repositório e consistência com os dados locais verificados.

**Gate 2 — Motor**: passar testes C# determinísticos e verificações
estáticas, incluindo os cenários regressivos dos protótipos anteriores.

**Gate 3 — Unity Editor**: abrir a versão correta 6000.3.28f1,
zerar os erros de compilação do Console, executar os testes EditMode,
iniciar Play e repetir o cenário relatado pelo usuário.

**Gate 4 — Jogabilidade**: pelo menos um teste por mouse e outro por
teclado; incluir cancelamento, Face Down, transição de turno e
efeito/cemitério se a mudança os afetar.

**Gate 5 — Distribuição**: criar o backup na mesma SHA de código
testado, manter o PR revisável, divulgar o ZIP da branch verificada,
explicar claramente o que foi testado e o que ainda está pendente.
Não chamar código-fonte Unity de executável pronto nem afirmar
validação visual sem execução no Game View.

## Quando fusões falharem

1. Conferir fonte do catálogo e SHA (mesma ISO do usuário).
2. Verificar o total de receitas disponíveis em
   `original_export.json` pelo diagnóstico de fusões.
3. Confirmar pares e IDs específicos; não deduzir receitas por nome.
4. Reproduzir no motor: duas cartas de mão, cartas em ordem inversa,
   material sobre monstro em campo, falha de receita e descarte.
5. Reproduzir no Unity: seleção por mouse, seleção ↑ com teclado,
   posição do slot e animação de resultado.
6. Corrigir apenas a camada com evidência de falha.

## Critério de comunicação

Cada entrega precisa declarar explicitamente:
- Versão, branch, commit/backup e link do projeto.
- Testes automatizados que passaram **e os que não foram executados**.
- Mudanças concretas, incompatibilidades, conteúdo privado necessário.
- Caminho de reprodução para um eventual bug persistente.

Esse protocolo é uma regra de desenvolvimento, não uma alegação
de que todos os testes do Unity já foram executados.
