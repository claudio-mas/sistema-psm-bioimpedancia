# Migração do Sistema PSM para web — decisões de brainstorming

Data: 2026-09-27.

Status: consolidação revisada e aprovada pelo usuário. Este documento registra decisões da conversa e propostas ainda sujeitas a detalhamento. A aprovação permite avançar na especificação; não autoriza implementação nem substitui a validação das regras extraídas do sistema atual.

Detalhamento para revisão: [catálogo de regras, dicionário de dados e divergências](../../migracao-web/README.md).

## 1. Objetivo e critérios de sucesso

Substituir a operação em Bioimpedancia.xlsm por uma aplicação SPA para uma clínica, hospedada na nuvem. Preservar as regras dos protocolos utilizados, os conteúdos dos documentos e a identidade visual do laudo. O sistema deverá suportar até cinco usuários simultâneos.

A migração será considerada funcionalmente validada quando os cenários acordados produzirem resultados compatíveis com o sistema atual, com diferenças aprovadas e documentadas, e os usuários conseguirem executar cadastro, anamnese, avaliação, revisão e emissão de documentos.

## 2. Decisões confirmadas pelo usuário

| Tema | Decisão |
|---|---|
| Operação | Uma clínica, hospedada na nuvem |
| Simultaneidade | Até cinco usuários |
| Frontend | SPA em React + TypeScript |
| Backend | NestJS |
| Banco | PostgreSQL |
| Migração de dados | Somente cadastros de pacientes; novas anamneses e avaliações no sistema web |
| Consulta do passado | Planilha como referência do histórico anterior |
| Perfis | Administrador, profissional de saúde e assistente |
| Assistente | Permissões configuráveis pelo administrador; padrão de preenchimento supervisionado |
| Protocolos | A, B/R e C na primeira versão |
| OCR | Incluído na primeira versão, exclusivamente para o modelo atual de PDF Relaxmedic |
| Documentos | Avaliação detalhada, evolução/comparações, anamnese e ficha cadastral |
| Alterações após emissão | Revisões com histórico |
| Visual das telas | Seguir a identidade visual do laudo |
| Layout dos documentos | Pode ser reorganizado, preservando conteúdo e identidade visual |
| Possibilidade futura | Preparar a arquitetura para um template fiel ao layout original |

## 3. Fontes para especificar o comportamento

- `Bioimpedancia.xlsm`: fonte atual dos formulários, dados e regras VBA. A inspeção do XML confirmou 25 abas e 21 fórmulas de célula em Analise C.
- `SistemaPSM.AddIn`: código C# de leitura, cálculo complementar, apresentação, geração de PDF e importação OCR.
- `PRD.md`: catálogo inicial de campos e regras, produzido em contexto de proposta de micro-SaaS. Suas premissas de produto devem ser reconciliadas com este documento.
- `DESIGN.md` e `SistemaPSM.AddIn/Laudo/Template*.html`: referências de identidade visual e apresentação.
- `_prd_evidence`: extrações históricas. O arquivo olevba.txt inspecionado é anterior ao XLSM atual; sua equivalência ao VBA atual não está confirmada.

As regras estão distribuídas entre VBA, fórmulas de célula e C#. O C# também calcula informações de apresentação, controles de gordura/músculo, tipo corporal e seleção do histórico. Extrair apenas as fórmulas de célula seria insuficiente.

## 4. Arquitetura proposta para revisão

Uma API organizada em módulos: acesso e permissões, pacientes, anamneses, avaliações, regras de cálculo, importação, documentos e administração.

Fluxo principal: navegador → API → PostgreSQL e armazenamento privado. A API valida permissões e confirma cálculos antes de persistir resultados. O navegador pode exibir prévias, mas resultados enviados pelo cliente devem ser conferidos no servidor.

Propostas complementares à stack já aprovada:

| Finalidade | Proposta |
|---|---|
| Construção da SPA | Vite e React Router |
| Componentes e tema | Tailwind CSS e shadcn/ui |
| Consultas e sincronização | TanStack Query |
| Acesso ao banco | Prisma ORM, com migrations versionadas |
| PDF | HTML/CSS e Playwright/Chromium no servidor |
| Hospedagem | Aplicação com HTTPS, PostgreSQL gerenciado e armazenamento privado |
| Autenticação | Contas individuais; mecanismo de identidade/sessão ainda a escolher |

Até cinco usuários permite propor uma implantação inicial pequena. A capacidade efetiva depende também do custo e da frequência de OCR e PDF. Esses trabalhos terão concorrência limitada e estados explícitos de processamento. A necessidade de um processo separado será avaliada com medições na implementação.

Prever detecção de edição concorrente: se duas pessoas editarem a mesma avaliação, uma atualização não poderá apagar silenciosamente a outra. A estratégia proposta é verificar a versão do registro ao salvar.

Operação dependente de internet é uma premissa técnica a confirmar; trabalho offline não foi solicitado. Provedor, região, orçamento, recursos de máquina e metas de disponibilidade ainda não foram definidos.

## 5. Permissões e revisões

O administrador gerencia usuários, configurações e permissões. O profissional realiza o atendimento e emite documentos clínicos. O assistente começa com preenchimento supervisionado.

A configuração do assistente deve contemplar, separadamente: cadastro de pacientes; rascunhos de anamneses; rascunhos de avaliações; envio e revisão de OCR; consulta de histórico; acesso a documentos emitidos. Finalização e emissão clínica ficam reservadas ao profissional/administrador por padrão. A possibilidade de delegar essas últimas ações ainda precisa ser definida na matriz final.

Todas as permissões devem ser verificadas na API, inclusive acesso a anexos. Mudanças de permissão e revisões clínicas devem registrar autor e data.

Proposta para avaliações emitidas: manter revisões identificáveis, registrar campos alterados e associar cada documento à revisão, versão das regras e versão do template que o originou. Preservar os PDFs anteriores. Política de exclusão, retenção e restauração ainda precisa ser definida.

## 6. Regras e modelo de dados

Inventariar cada regra com identificador, localização da fonte, protocolo, entradas, unidades, fórmula ou decisão, limites, arredondamento, valores ausentes, saída e exemplos de referência.

Cobertura necessária: idade e datas; validações de cadastro; anamnese; medidas antropométricas; composição corporal; classificações; IMC e RCQ; métodos de TMB e GET; metas; medidas segmentares; impedância; resultados derivados do laudo; seleção do histórico comparativo.

Cuidados já identificados:

- Diferenciar classificação manual, importada e calculada.
- Distinguir campo ausente de zero e de não aplicável.
- Documentar unidades: água e proteína são armazenadas em percentual no fluxo atual; outros componentes têm campos de kg e percentual.
- Conferir os efeitos de Round e Format do VBA e a ordem dos cálculos.
- Conferir a data usada para idade e a preservação da idade na avaliação histórica.
- O laudo comparativo atual seleciona a primeira e as cinco avaliações mais recentes quando há mais de seis avaliações elegíveis.
- O PRD relata possível inconsistência de unidade no IMC interno das funções DRI. Verificar o VBA atual e submeter qualquer mudança de comportamento a decisão explícita.
- Registrar separadamente o comportamento observado, as divergências encontradas e o comportamento aprovado para o novo sistema.

Entidades candidatas: paciente, usuário, profissional, anamnese, avaliação, revisão da avaliação, medidas segmentares, impedâncias, importação OCR, documento emitido, anexo e registro de auditoria. O esquema ainda será detalhado a partir do dicionário de dados.

Preservar entradas e resultados por avaliação, com a versão de regra utilizada. Armazenar medidas com unidades explícitas e precisão definida por campo. Uma alteração futura de regra não deve recalcular silenciosamente documentos anteriores.

## 7. Importação dos cadastros

Mapear os campos da aba Clientes, preservando o identificador legado como referência. Fazer prévia e conferência antes da importação definitiva, com relatório de aceitos, rejeitados e possíveis duplicidades. Definir tratamento de repetição da importação para evitar novos cadastros duplicados.

Confirmar quais campos serão efetivamente mantidos, como identificar duplicidades e se fotos e anexos cadastrais entram na carga. Anamneses e avaliações antigas estão fora da carga inicial acordada. A necessidade de disponibilidade da planilha antiga e seus arquivos associados deve ser considerada no procedimento de transição.

## 8. OCR do Relaxmedic

Fluxo proposto: enviar PDF → validar o arquivo → identificar o modelo esperado → extrair os valores → apresentar revisão → confirmar na avaliação.

O suporte é restrito ao modelo atual. Os valores reconhecidos devem manter sua origem identificável, com campos ausentes ou suspeitos destacados. A revisão pelo usuário é parte do fluxo. A proposta é vincular o PDF original à avaliação.

O componente atual usa Windows.Media.Ocr por PowerShell e regiões fixas do relatório. O motor para nuvem ainda não foi escolhido. Avaliar a qualidade com PDFs representativos do mesmo modelo, latência, custo, dependência de sistema operacional e eventual envio de dados a fornecedor externo antes de fechar a tecnologia.

Precisam ser definidos os limites de arquivo, estados de falha, possibilidade de nova tentativa e política de retenção dos originais. OCR não deve sobrescrever silenciosamente campos já preenchidos.

## 9. Documentos e identidade visual

Os quatro documentos devem ter seus conteúdos e variantes mapeados contra os protocolos aplicáveis. Reorganização visual está autorizada; preservação de conteúdo e identidade é requisito.

Paleta de referência: alabastro #FDFBF7, grafite #1F1A17, terracota #7A2B22, ouro #C5A059, oliva #8A9A70 e areia #E2DCD3. Playfair Display em títulos; Inter em formulários, tabelas e textos. Validar contraste nos usos reais de cada combinação.

Proposta de navegação: página do paciente com cadastro, anamnese, avaliações e documentos; entrada única para nova avaliação com seleção entre A, B/R e C; seções específicas por protocolo; unidades sempre visíveis; revisão antes da emissão.

Separar conteúdo estruturado e cálculos dos templates. Preservar fontes, imagens, templates e PDFs representativos do sistema atual. Uma futura versão fiel ao original terá implementação e homologação próprias; preparar essa separação não significa que o template original já estará disponível na primeira versão.

PDFs emitidos permanecerão associados ao template utilizado. A mudança de wkhtmltopdf para Chromium exige verificar fontes, cores, tabelas e paginação.

## 10. Validação e entrada em operação

Preparar casos de referência por protocolo com resultados obtidos no sistema atual. Incluir limites de classificação, faixas etárias, valores ausentes, zero, separador decimal, arredondamentos, avaliações na mesma data e histórico com mais de seis registros.

Comparar resultados de cálculo e documentos. Definir quais saídas exigem igualdade exata e quais admitem tolerância numérica aprovada. Diferenças conhecidas devem possuir justificativa e aceite explícito.

Verificar permissões por ação, conflitos de edição, revisões, importação repetida, falhas de OCR/PDF e acesso aos arquivos privados. Validar visualmente os quatro documentos e suas variantes pertinentes.

Definir backup do banco e arquivos, responsável operacional, frequência, retenção e teste de restauração. Planejar corte de uso, conferência dos cadastros importados, treinamento e procedimento de retorno ao sistema anterior.

## 11. Próximos entregáveis de especificação

1. Inventário atualizado do VBA, propriedades dos formulários, fórmulas e regras C#.
2. Catálogo rastreável de regras e registro das divergências.
3. Dicionário dos campos e proposta de modelo relacional.
4. Matriz de permissões e fluxo de revisões.
5. Mapeamento dos quatro documentos e dos campos OCR.
6. Proposta de telas e navegação seguindo a identidade do laudo.
7. Casos de referência e critérios de homologação.
8. Definição operacional de hospedagem, custos, internet, backup e transição.

Este documento consolida o brainstorming. O código da aplicação, a planilha e a publicação permanecem fora desta etapa.
