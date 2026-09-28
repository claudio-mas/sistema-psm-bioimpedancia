# Evidências, fórmulas de célula e OCR

Arquivo de referência: `Bioimpedancia.xlsm`; SHA-256 `c1f34adaec04a8303c15970c376af7b400490e5fa51d54f317557bbc564ac773`.

Extração: 89 módulos, 651 procedimentos, 25 abas. Foi feita leitura estática; macros não foram executadas.

## Limites da extração

A contagem de procedimentos é textual (Sub/Function/Property), não uma prova de cobertura de execução. A extração de 1.556 controles retornou várias legendas com fragmentos e caracteres de controle; legendas/propriedades visuais não são consideradas verificadas. A documentação anterior de captions é referência histórica, a conferir no Excel.

As evidências temporárias completas e o extrator ficam em `_Teste/migracao-2026-09-27/`, ignorado pelo Git. Este pacote mantém a identificação da fonte, o inventário, fórmulas e transcrições de regras para revisão sem expor registros de pacientes.

## Inventário VBA

| Módulo | Linhas | Procedimentos |
|---|---:|---:|
| EstaPastaDeTrabalho.cls | 16 | 2 |
| Planilha1.cls | 8 | 0 |
| FrmTutorial.frm | 313 | 10 |
| Menu.bas | 60 | 10 |
| Planilha2.cls | 8 | 0 |
| Planilha3.cls | 8 | 0 |
| Planilha4.cls | 8 | 0 |
| BackUp.bas | 19 | 2 |
| Vazio.bas | 72 | 3 |
| FrmInformaçoes.frm | 124 | 4 |
| Planilha7.cls | 10 | 0 |
| FrmCadastroPL_C.frm | 1490 | 36 |
| MeuCadastro.bas | 72 | 3 |
| VariaveisGlobais.bas | 5 | 0 |
| Clientes.bas | 73 | 4 |
| TELA_.bas | 188 | 11 |
| FrmCliente.frm | 818 | 18 |
| Planilha6.cls | 9 | 0 |
| FrmCadastro_C.frm | 684 | 17 |
| MenuPainel.bas | 43 | 10 |
| FrmSuporte.frm | 55 | 2 |
| Logo.bas | 104 | 3 |
| FrmLogo.frm | 43 | 3 |
| FrmFoto.frm | 9 | 0 |
| Idade.bas | 14 | 1 |
| FrmAvaliacaoB.frm | 847 | 68 |
| Planilha5.cls | 8 | 0 |
| FrmBioimpedancia.frm | 52 | 5 |
| FrmBioimpR.frm | 260 | 19 |
| FrmHoutkooperTabela.frm | 9 | 0 |
| FrmPollockTabela.frm | 9 | 0 |
| FrmMusculoEsqueletico.frm | 8 | 0 |
| FrmGorduraOmron.frm | 8 | 0 |
| FrmVisceral.frm | 8 | 0 |
| FrmIMComr.frm | 8 | 0 |
| FrmNecessidadeEnergetica.frm | 292 | 19 |
| FrmRelatorioB.frm | 362 | 11 |
| FrmCalcPeso.frm | 297 | 23 |
| AvaliacaoB.bas | 953 | 8 |
| Planilha25.cls | 10 | 0 |
| Comparacoes_.bas | 4223 | 16 |
| Energia.bas | 131 | 4 |
| Equações_Avaliações.bas | 309 | 14 |
| IMC.bas | 2522 | 3 |
| GORDURA.bas | 835 | 3 |
| MUSCULOS.bas | 63 | 1 |
| Rcq.bas | 311 | 2 |
| FrmAvaliacoesBR.frm | 9 | 0 |
| FrmRCQ.frm | 8 | 0 |
| FrmInfos.frm | 10 | 0 |
| Planilha8.cls | 9 | 0 |
| Planilha10.cls | 9 | 0 |
| Planilha9.cls | 9 | 0 |
| FrmCalculo.frm | 9 | 0 |
| FrmBioimpC.frm | 1026 | 62 |
| FrmAvaliacaoC.frm | 1089 | 91 |
| AvaliacaoC.bas | 1384 | 11 |
| Planilha12.cls | 9 | 0 |
| Planilha11.cls | 9 | 0 |
| Planilha13.cls | 9 | 0 |
| Planilha14.cls | 8 | 0 |
| Planilha15.cls | 10 | 0 |
| FrmLogo_.frm | 44 | 3 |
| Painel.bas | 327 | 6 |
| Planilha16.cls | 10 | 0 |
| FrmAvaliacaoA.frm | 901 | 63 |
| FrmIMC.frm | 8 | 0 |
| FrmPollock.frm | 8 | 0 |
| AvaliacaoA.bas | 1078 | 11 |
| FrmAvaliacoesBR_.frm | 10 | 0 |
| Planilha17.cls | 9 | 0 |
| Planilha18.cls | 9 | 0 |
| Planilha19.cls | 9 | 0 |
| Planilha20.cls | 10 | 0 |
| Planilha22.cls | 8 | 0 |
| FrmResidual.frm | 8 | 0 |
| FrmMuscular.frm | 8 | 0 |
| Anamnese.bas | 742 | 5 |
| Planilha24.cls | 9 | 0 |
| Planilha21.cls | 9 | 0 |
| Planilha23.cls | 9 | 0 |
| FrmUrina.frm | 92 | 4 |
| FrmAnexo.frm | 155 | 4 |
| FrmAgua.frm | 96 | 9 |
| FrmAnamnese.frm | 413 | 28 |
| FrmFcMax.frm | 122 | 5 |
| FrmPaCls.frm | 9 | 0 |
| FrmRelatorioAn.frm | 366 | 10 |
| FiltrosLTB.bas | 278 | 4 |

## Fórmulas de célula — Analise C

G7 usa a fórmula compartilhada de G6:G7: a fórmula resolvida é `=1-F7`. O `=` da documentação antiga resultou da leitura incompleta do XML, não de uma fórmula vazia no Excel. Valores em cache não foram usados como comprovação de recálculo.

| Célula | Fórmula resolvida |
|---|---|
| G5 | `=1-F5` |
| G6 | `=1-F6` |
| G7 | `=1-F7` |
| G8 | `=1-F8` |
| H11 | `=$D$12` |
| O11 | `=IF($G$2="Masculino",P11,Q11)` |
| O12 | `=IF($G$2="Masculino",P12,Q12)` |
| H13 | `=SUM($L$11:$L$15)-$H$11-$H$12` |
| O13 | `=IF($G$2="Masculino",P13,Q13)` |
| O14 | `=IF($G$2="Masculino",P14,Q14)` |
| L15 | `=SUM(L11:L14)` |
| O15 | `=SUM(O11:O14)` |
| H16 | `=$F$5` |
| H18 | `=SUM($O$11:$O$15)-$H$16-$H$17` |
| H22 | `=$F$5` |
| H24 | `=SUM($O$27:$O$30)-$H$22-$H$23` |
| O27 | `=IF($G$2="Masculino",P27,Q27)` |
| O28 | `=IF($G$2="Masculino",P28,Q28)` |
| O29 | `=IF($G$2="Masculino",P29,Q29)` |
| E30 | `=1-D30` |
| O30 | `=SUM(O27:O29)` |

## Mapeamento OCR atual

Fonte: `SistemaPSM.AddIn/Importacao/RelatorioRelaxmedicLayout.cs`. Coordenadas por mil da largura/altura. `Baixa` é uma marca estática da zona, não uma probabilidade de acerto calculada pelo motor. Não extrai identidade, sexo, idade, altura ou data do paciente.

| Controle VBA de destino | Retângulo x0,y0,x1,y1 | Formato | Conferência reforçada |
|---|---|---|---|
| `TxtPeso` | 150,136,250,153 | Dec2 | Não (revisão continua obrigatória) |
| `TxtGorduraAtualKg` | 150,157,250,173 | Dec2 | Não (revisão continua obrigatória) |
| `TxtMassaOssea` | 150,177,250,193 | Dec2 | Não (revisão continua obrigatória) |
| `TxtProteinaKg` | 150,197,250,213 | Dec2 | Não (revisão continua obrigatória) |
| `TxtAguaCorporalKg` | 150,217,250,233 | Dec2 | Não (revisão continua obrigatória) |
| `TxtMassaMuscular` | 150,238,250,254 | Dec2 | Não (revisão continua obrigatória) |
| `TxtPesoMuscularEsq` | 150,258,250,275 | Dec2 | Não (revisão continua obrigatória) |
| `TxtPontuacao` | 600,122,690,150 | Int | Não (revisão continua obrigatória) |
| `TxtImc` | 780,360,865,379 | Dec2 | Não (revisão continua obrigatória) |
| `TxtGorduraVisceral` | 860,853,965,869 | Int | Não (revisão continua obrigatória) |
| `TxtTmB` | 860,872,965,888 | Int | Não (revisão continua obrigatória) |
| `TxtMlg` | 860,893,965,907 | Dec2 | Não (revisão continua obrigatória) |
| `TxtGorduraSubcutanea` | 860,911,965,927 | Dec2 | Não (revisão continua obrigatória) |
| `TxtIdadeCorpo` | 860,951,965,966 | Int | Não (revisão continua obrigatória) |
| `TxtZ20BracoD1` | 168,915,238,931 | Dec1 | Não (revisão continua obrigatória) |
| `TxtZ20BracoE` | 248,915,320,931 | Dec1 | Não (revisão continua obrigatória) |
| `TxtZ20Tronco` | 333,915,392,931 | Dec1 | Não (revisão continua obrigatória) |
| `TxtZ20PernaD` | 408,915,472,931 | Dec1 | Não (revisão continua obrigatória) |
| `TxtZ20PernaE` | 488,915,548,931 | Dec1 | Não (revisão continua obrigatória) |
| `TxtZ100BracoD1` | 168,941,238,957 | Dec1 | Não (revisão continua obrigatória) |
| `TxtZ100BracoE` | 248,941,320,957 | Dec1 | Não (revisão continua obrigatória) |
| `TxtZ100Tronco` | 333,941,392,957 | Dec1 | Não (revisão continua obrigatória) |
| `TxtZ100PernaD` | 408,941,472,957 | Dec1 | Não (revisão continua obrigatória) |
| `TxtZ100PernaE` | 488,941,548,957 | Dec1 | Não (revisão continua obrigatória) |
| `TxtPesoBracoEsquerdo` | 35,605,130,620 | Dec2 | Sim |
| `TxtPorcentagemBracoEsquerdo` | 35,621,130,637 | Dec2 | Sim |
| `TxtPesoBracoDireito` | 205,605,295,620 | Dec2 | Sim |
| `TxtPorcentagemBracoDireito` | 205,621,295,637 | Dec2 | Sim |
| `TxtPesoAbdominal` | 35,659,130,674 | Dec2 | Sim |
| `TxtPorcentagemAbdominal` | 35,675,130,691 | Dec2 | Sim |
| `TxtPesoPernaEsquerda` | 35,724,130,740 | Dec2 | Sim |
| `TxtPorcentagemPernaEsquerda` | 35,740,130,756 | Dec2 | Sim |
| `TxtPesoPernaDireita` | 205,724,295,740 | Dec2 | Sim |
| `TxtPorcentagemPernaDireita` | 205,740,295,756 | Dec2 | Sim |
| `TxtMuscBracoEsqKg` | 315,605,400,620 | Dec2 | Sim |
| `TxtMuscBracoEsqPct` | 315,621,400,637 | Dec2 | Sim |
| `TxtMuscBracoDirKg` | 510,605,560,620 | Dec2 | Sim |
| `TxtMuscBracoDirPct` | 500,621,560,637 | Dec2 | Sim |
| `TxtMuscTroncoKg` | 315,659,400,674 | Dec2 | Sim |
| `TxtMuscTroncoPct` | 315,675,400,691 | Dec2 | Sim |
| `TxtMuscPernaEsqKg` | 315,724,400,740 | Dec2 | Sim |
| `TxtMuscPernaEsqPct` | 315,740,400,756 | Dec2 | Sim |
| `TxtMuscPernaDirKg` | 510,724,560,740 | Dec2 | Sim |
| `TxtMuscPernaDirPct` | 500,740,560,756 | Dec2 | Sim |

Total atual: 44 zonas; 20 marcadas como baixa confiança. Dec1/Dec2/Int controlam formatação; o VBA depois pode arredondar novamente, como nas impedâncias.

## Hashes das fontes C# consultadas

| Arquivo | SHA-256 |
|---|---|
| Laudo/LaudoRepositorio.cs | `ecbbdb5099a7d1c09e725c6638350acfd4a86cd9f9a070716d05eb21900dd26b` |
| Laudo/LaudoAvaliacaoHtmlBuilder.cs | `3125ca44d2b35ef8512b5df2b9be3e7125755f5d8e6ff648e17e6b9b9779c050` |
| Laudo/LaudoHtmlBuilder.cs | `4c48462fc2300a53272d9d715b607d42ca5c2a7b532fc8deaf136ac77001f7f0` |
| Laudo/AvaliacaoLaudo.cs | `7c7b91ddd7094020c71ffac194facb89efa4e43d8b3b3b3e537cb0b87088d530` |
| Importacao/RelatorioRelaxmedicLayout.cs | `fbdf5d919cfa31f974794e6260b7e63f40d27386a2a1c0db3db9b1ef6f7194c5` |
| Importacao/BioimpedanciaOcrService.cs | `93c8396d411194a1ebc713809ab921a89f6355906df5c2c74db302977a260b05` |
