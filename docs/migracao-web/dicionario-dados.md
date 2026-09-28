# Dicionário de dados — legado e proposta web

Status: mapeamento estático do arquivo atual; nomes e tipos web são propostas, sem schema implementado.
Fonte: `Bioimpedancia.xlsm`, SHA-256 `c1f34adaec04a8303c15970c376af7b400490e5fa51d54f317557bbc564ac773`. Nenhum registro pessoal foi copiado para este documento.

## Convenções

- Coluna é a posição física no XLSM; offset começa em zero. As expressões representam gravações encontradas, não todas as condições de execução.
- “—” significa nenhuma gravação encontrada nas rotinas de salvamento inspecionadas, sem afirmar que o campo é impossível de preencher por outro caminho.
- Campos numéricos são propostas para `numeric`, com escala final definida pela regra; texto numérico de CPF, RG, CEP e telefone permanece texto.
- Nulabilidade e obrigatoriedade clínica dependem do protocolo. Não tornar todas as colunas obrigatórias. Preservar diferença entre ausente, zero e não aplicável.
- IDs da aplicação serão independentes de `legacy_id`. Nomes/idade armazenados na avaliação representam o contexto histórico; referências ao paciente não devem reescrever laudos anteriores.

## Pacientes — Clientes (25 colunas)

Carga inicial aprovada: cadastros. Inclusão de fotos/anexos ainda depende de decisão. O campo Y aponta para anexo de anamnese; não importá-lo automaticamente como parte do cadastro, pois histórico clínico não faz parte da carga aprovada. Nome, sexo e preenchimento do nascimento são exigidos pelo botão de salvar atual; validade da data é tratada em outro trecho e pode terminar em vazio.

| Coluna / offset | Cabeçalho atual | Campo proposto | Tipo | Gravação observada |
|---|---|---|---|---|
| A / 0 | Seq. | `legacy_id` | inteiro | `CInt(TxtSeq)` (FrmCadastro_C.frm:239) |
| B / 1 | Nome | `full_name` | texto | `Application.WorksheetFunction.Trim(txtNome)` (FrmCadastro_C.frm:241) |
| C / 2 | Sexo | `sex_for_reference` | texto | `CbSexo` (FrmCadastro_C.frm:244) |
| D / 3 | Nascimento | `birth_date` | date | `CDate(TxtData)` (FrmCadastro_C.frm:250) |
| E / 4 | Idade | `age_legacy` | inteiro | `CInt(TxtIdade)` (FrmCadastro_C.frm:257) |
| F / 5 | Profissao | `profession` | texto | `TxtProfissao` (FrmCadastro_C.frm:265) |
| G / 6 | Foto Anexo | `photo_file` | referência de arquivo | `caminhoDestino5` (FrmCadastro_C.frm:279) |
| H / 7 | Telefone | `phone` | texto | `TxtTelefone` (FrmCadastro_C.frm:287) |
| I / 8 | Celular | `mobile` | texto | `TxtCelular` (FrmCadastro_C.frm:288) |
| J / 9 | Email | `email` | texto | `TxtEmail` (FrmCadastro_C.frm:289) |
| K / 10 | Observações Gerais | `notes` | texto | `TxtObs` (FrmCadastro_C.frm:291) |
| L / 11 | Cep | `postal_code` | texto | `TxtCep` (FrmCadastro_C.frm:293) |
| M / 12 | Logradouro | `street` | texto | `TxtLogradouro` (FrmCadastro_C.frm:295) |
| N / 13 | Numero | `street_number` | texto | `CDbl(TxtNumero)` (FrmCadastro_C.frm:298) |
| O / 14 | Complemento | `address_complement` | texto | `CDbl(TxtComplemento)` (FrmCadastro_C.frm:304) |
| P / 15 | Bairro | `district` | texto | `TxtBairro` (FrmCadastro_C.frm:309) |
| Q / 16 | Município | `city` | texto | `TxtMunicipio` (FrmCadastro_C.frm:310) |
| R / 17 | UF | `state` | texto | `LtbUf` (FrmCadastro_C.frm:313) |
| S / 18 | Rg | `rg` | texto | `CDbl(TxtRg)` (FrmCadastro_C.frm:321) |
| T / 19 | Rg Anexo | `rg_file` | referência de arquivo | `caminhoDestino1` (FrmCadastro_C.frm:332) |
| U / 20 | Cpf | `cpf` | texto | `CDbl(TxtCpf)` (FrmCadastro_C.frm:343) |
| V / 21 | Cpf Anexo | `cpf_file` | referência de arquivo | `caminhoDestino2` (FrmCadastro_C.frm:355) |
| W / 22 | Outro | `other_document` | texto | `CDbl(TxtOutro)` (FrmCadastro_C.frm:366) |
| X / 23 | Outro Anexo | `other_document_file` | referência de arquivo | `caminhoDestino4` (FrmCadastro_C.frm:378) |
| Y / 24 | Anamnese Anexo | `anamnesis_file` | referência de arquivo | — |

## Avaliações — 108 posições A:DD

Os três fluxos compartilham a mesma aba. DD não possui cabeçalho no XML atual: `SalvaAvaliacaoC` e `OFF_HORA` confirmam hora da medição. No fluxo C, água/proteína são % no armazenamento; o kg é derivado no AddIn. Os percentuais segmentares devem permanecer distintos dos percentuais de composição do peso.

| Coluna / offset | Cabeçalho atual | Campo proposto | Tipo/unidade | Gravação A | Gravação B/R | Gravação C |
|---|---|---|---|---|---|---|
| A / 0 | Seq. | `legacy_id` | inteiro | `CInt(FrmAvaliacaoA.TxtCodA)` (AvaliacaoA.bas:8) | `CInt(FrmAvaliacaoB.TxtCodA)` (AvaliacaoB.bas:8) | `CInt(FrmAvaliacaoC.TxtCodA)` (AvaliacaoC.bas:33) |
| B / 1 | Data Aval. | `assessed_on` | date | `Data` (FrmAvaliacaoA.frm:183) | `Data` (FrmAvaliacaoB.frm:110) | `Data` (FrmAvaliacaoC.frm:109) |
| C / 2 | Mês | `month_label` | texto | `mesMaiusculo` (FrmAvaliacaoA.frm:185) | `mesMaiusculo` (FrmAvaliacaoB.frm:112) | `mesMaiusculo` (FrmAvaliacaoC.frm:111) |
| D / 3 | Ano | `year` | inteiro | `ano` (FrmAvaliacaoA.frm:186) | `ano` (FrmAvaliacaoB.frm:113) | `ano` (FrmAvaliacaoC.frm:112) |
| E / 4 | Id | `patient_legacy_id` | inteiro | `CInt(FrmAvaliacaoA.TxtCodC)` (AvaliacaoA.bas:9) | `CInt(FrmAvaliacaoB.TxtCodC)` (AvaliacaoB.bas:9) | `CInt(FrmAvaliacaoC.TxtCodC)` (AvaliacaoC.bas:34) |
| F / 5 | Nome | `patient_name` | texto | `FrmAvaliacaoA.txtNome` (AvaliacaoA.bas:10) | `FrmAvaliacaoB.txtNome` (AvaliacaoB.bas:10) | `FrmAvaliacaoC.txtNome` (AvaliacaoC.bas:35) |
| G / 6 | Sexo | `sex_for_reference` | texto | `FrmAvaliacaoA.TxtSexo` (AvaliacaoA.bas:11) | `FrmAvaliacaoB.TxtSexo` (AvaliacaoB.bas:11) | `FrmAvaliacaoC.TxtSexo` (AvaliacaoC.bas:36) |
| H / 7 | Idade | `age_years` | inteiro | `CInt(FrmAvaliacaoA.TxtIdade)` (AvaliacaoA.bas:12) | `CInt(FrmAvaliacaoB.TxtIdade)` (AvaliacaoB.bas:12) | `CInt(FrmAvaliacaoC.TxtIdade)` (AvaliacaoC.bas:37) |
| I / 8 | Faixa etaria | `age_band` | texto | `"Crianças"` (AvaliacaoA.bas:16) | `"Crianças"` (AvaliacaoB.bas:16) | `"Crianças"` (AvaliacaoC.bas:42) |
| J / 9 | Peso | `weight_kg` | decimal; kg | `CDbl(FrmAvaliacaoA.TxtPesoKg)` (AvaliacaoA.bas:27) | `CDbl(FrmAvaliacaoB.TxtPesoKg)` (AvaliacaoB.bas:27) | `CDbl(FrmAvaliacaoC.TxtPesoKg)` (AvaliacaoC.bas:53) |
| K / 10 | Altura | `height_cm` | decimal; cm | `CDbl(FrmAvaliacaoA.TxtAltura)` (AvaliacaoA.bas:33) | `CDbl(FrmAvaliacaoB.TxtAltura)` (AvaliacaoB.bas:33) | `CDbl(FrmAvaliacaoC.TxtAltura)` (AvaliacaoC.bas:59) |
| L / 11 | Tipo | `type` | texto | `"Bioimpedância"` (AvaliacaoA.bas:39) | `"Bioimpedância"` (AvaliacaoB.bas:39) | `"Bioimpedância"` (AvaliacaoC.bas:65) |
| M / 12 | Protocolo | `protocol` | texto | `FrmAvaliacaoA.TxtProtocolo` (AvaliacaoA.bas:42) | `FrmAvaliacaoB.TxtProtocolo` (AvaliacaoB.bas:42) | `FrmAvaliacaoC.TxtProtocolo` (AvaliacaoC.bas:68) |
| N / 13 | Observação | `observation` | texto | `FrmAvaliacaoA.TxtObs` (AvaliacaoA.bas:44) | `FrmAvaliacaoB.TxtObs` (AvaliacaoB.bas:44) | `FrmAvaliacaoC.TxtObs` (AvaliacaoC.bas:70) |
| O / 14 | Data Periodo | `period_date` | date | `Data` (FrmAvaliacaoA.frm:184) | `Data` (FrmAvaliacaoB.frm:111) | `Data` (FrmAvaliacaoC.frm:110) |
| P / 15 | Pescoco | `neck_cm` | decimal; cm | `CDbl(FrmAvaliacaoA.TxtPescoco)` (AvaliacaoA.bas:48) | `CDbl(FrmAvaliacaoB.TxtPescoco)` (AvaliacaoB.bas:48) | `CDbl(FrmAvaliacaoC.TxtPescoco)` (AvaliacaoC.bas:74) |
| Q / 16 | Ombros | `shoulders_cm` | decimal; cm | `CDbl(FrmAvaliacaoA.TxtOmbros)` (AvaliacaoA.bas:54) | `CDbl(FrmAvaliacaoB.TxtOmbros)` (AvaliacaoB.bas:54) | `CDbl(FrmAvaliacaoC.TxtOmbros)` (AvaliacaoC.bas:80) |
| R / 17 | Peitoral | `chest_cm` | decimal; cm | `CDbl(FrmAvaliacaoA.TxtPeitoral)` (AvaliacaoA.bas:60) | `CDbl(FrmAvaliacaoB.TxtPeitoral)` (AvaliacaoB.bas:60) | `CDbl(FrmAvaliacaoC.TxtPeitoral)` (AvaliacaoC.bas:86) |
| S / 18 | Braco D | `arm_right_cm` | decimal; cm | `CDbl(FrmAvaliacaoA.TxtBracoDir)` (AvaliacaoA.bas:67) | `CDbl(FrmAvaliacaoB.TxtBracoDir)` (AvaliacaoB.bas:67) | `CDbl(FrmAvaliacaoC.TxtBracoDir)` (AvaliacaoC.bas:93) |
| T / 19 | Braco E | `arm_left_cm` | decimal; cm | `CDbl(FrmAvaliacaoA.TxtBracoEsq)` (AvaliacaoA.bas:73) | `CDbl(FrmAvaliacaoB.TxtBracoEsq)` (AvaliacaoB.bas:73) | `CDbl(FrmAvaliacaoC.TxtBracoEsq)` (AvaliacaoC.bas:99) |
| U / 20 | Antebraco D | `forearm_right_cm` | decimal; cm | `CDbl(FrmAvaliacaoA.TxtAntebracoDir)` (AvaliacaoA.bas:79) | `CDbl(FrmAvaliacaoB.TxtAntebracoDir)` (AvaliacaoB.bas:79) | `CDbl(FrmAvaliacaoC.TxtAntebracoDir)` (AvaliacaoC.bas:105) |
| V / 21 | Antebraco E | `forearm_left_cm` | decimal; cm | `CDbl(FrmAvaliacaoA.TxtAntebracoEsq)` (AvaliacaoA.bas:85) | `CDbl(FrmAvaliacaoB.TxtAntebracoEsq)` (AvaliacaoB.bas:85) | `CDbl(FrmAvaliacaoC.TxtAntebracoEsq)` (AvaliacaoC.bas:111) |
| W / 22 | Abdomen | `abdomen_cm` | decimal; cm | `CDbl(FrmAvaliacaoA.TxtAbdomen)` (AvaliacaoA.bas:92) | `CDbl(FrmAvaliacaoB.TxtAbdomen)` (AvaliacaoB.bas:92) | `CDbl(FrmAvaliacaoC.TxtAbdomen)` (AvaliacaoC.bas:118) |
| X / 23 | Cintura | `waist_cm` | decimal; cm | `CDbl(FrmAvaliacaoA.TxtCintura)` (AvaliacaoA.bas:99) | `CDbl(FrmAvaliacaoB.TxtCintura)` (AvaliacaoB.bas:99) | `CDbl(FrmAvaliacaoC.TxtCintura)` (AvaliacaoC.bas:125) |
| Y / 24 | Quadril | `hip_cm` | decimal; cm | `CDbl(FrmAvaliacaoA.TxtQuadril)` (AvaliacaoA.bas:106) | `CDbl(FrmAvaliacaoB.TxtQuadril)` (AvaliacaoB.bas:106) | `CDbl(FrmAvaliacaoC.TxtQuadril)` (AvaliacaoC.bas:132) |
| Z / 25 | Coxa D | `thigh_right_cm` | decimal; cm | `CDbl(FrmAvaliacaoA.TxtCoxaDir)` (AvaliacaoA.bas:113) | `CDbl(FrmAvaliacaoB.TxtCoxaDir)` (AvaliacaoB.bas:113) | `CDbl(FrmAvaliacaoC.TxtCoxaDir)` (AvaliacaoC.bas:139) |
| AA / 26 | Coxa E | `thigh_left_cm` | decimal; cm | `CDbl(FrmAvaliacaoA.TxtCoxaEsq)` (AvaliacaoA.bas:120) | `CDbl(FrmAvaliacaoB.TxtCoxaEsq)` (AvaliacaoB.bas:120) | `CDbl(FrmAvaliacaoC.TxtCoxaEsq)` (AvaliacaoC.bas:146) |
| AB / 27 | Pant D | `calf_right_cm` | decimal; cm | `CDbl(FrmAvaliacaoA.TxtPantDir)` (AvaliacaoA.bas:126) | `CDbl(FrmAvaliacaoB.TxtPantDir)` (AvaliacaoB.bas:126) | `CDbl(FrmAvaliacaoC.TxtPantDir)` (AvaliacaoC.bas:152) |
| AC / 28 | Pant E | `calf_left_cm` | decimal; cm | `CDbl(FrmAvaliacaoA.TxtPantEsq)` (AvaliacaoA.bas:133) | `CDbl(FrmAvaliacaoB.TxtPantEsq)` (AvaliacaoB.bas:133) | `CDbl(FrmAvaliacaoC.TxtPantEsq)` (AvaliacaoC.bas:159) |
| AD / 29 | IMC | `bmi` | decimal; kg/m² | `CDbl(FrmAvaliacaoA.TxtImc)` (AvaliacaoA.bas:152) | `CDbl(FrmAvaliacaoB.TxtImc)` (AvaliacaoB.bas:139) | `CDbl(FrmAvaliacaoC.TxtImc)` (AvaliacaoC.bas:165) |
| AE / 30 | Classificacao IMC | `bmi_classification` | texto | `FrmAvaliacaoA.TxtImcClassificacao` (AvaliacaoA.bas:158) | `FrmAvaliacaoB.TxtImcClassificacao` (AvaliacaoB.bas:145) | `FrmAvaliacaoC.TxtImcClassificacao` (AvaliacaoC.bas:170) |
| AF / 31 | % Gordura | `fat_pct` | decimal; % | `CDbl(FrmAvaliacaoA.TxtPGorduraAtual)` (AvaliacaoA.bas:162) | `CDbl(FrmAvaliacaoB.TxtPGorduraAtual)` (AvaliacaoB.bas:149) | `CDbl(FrmAvaliacaoC.TxtPGorduraAtual)` (AvaliacaoC.bas:174) |
| AG / 32 | Kg Peso Gordo | `fat_kg` | decimal; kg | `CDbl(FrmAvaliacaoA.TxtPesoGordo)` (AvaliacaoA.bas:168) | `CDbl(FrmAvaliacaoB.TxtPesoGordo)` (AvaliacaoB.bas:155) | `CDbl(FrmAvaliacaoC.TxtPesoGordo)` (AvaliacaoC.bas:180) |
| AH / 33 | Classificacao Gordura | `fat_classification` | texto | `FrmAvaliacaoA.TxtClassificacao` (AvaliacaoA.bas:173) | `FrmAvaliacaoB.TxtClassificacao` (AvaliacaoB.bas:160) | `FrmAvaliacaoC.TxtClassificacao` (AvaliacaoC.bas:185) |
| AI / 34 | Taxa Muscular | `muscle_pct` | decimal; % | `CDbl(FrmAvaliacaoA.TextBox43)` (AvaliacaoA.bas:177) | — | `CDbl(FrmAvaliacaoC.TxtTaxaMuscular)` (AvaliacaoC.bas:191) |
| AJ / 35 | Classificação Taxa Musc. | `muscle_pct_classification` | texto | — | — | `FrmAvaliacaoC.TxtTaxaMuscularClassificacao` (AvaliacaoC.bas:196) |
| AK / 36 | Peso Muscular | `muscle_kg` | decimal; kg | `CDbl(FrmAvaliacaoA.TxtPesoMuscular)` (AvaliacaoA.bas:183) | — | `CDbl(FrmAvaliacaoC.TxtMassaMuscular)` (AvaliacaoC.bas:199) |
| AL / 37 | Classificação Muscular | `muscle_kg_classification` | texto | — | — | `FrmAvaliacaoC.TxtMassaMuscularClassificacao` (AvaliacaoC.bas:204) |
| AM / 38 | % Massa Livre de Gordura | `fat_free_pct` | decimal; % | `CDbl(FrmAvaliacaoA.TextBox80)` (AvaliacaoA.bas:214) | `CDbl(FrmAvaliacaoB.TextBox43)` (AvaliacaoB.bas:166) | — |
| AN / 39 | Kg Massa Livre de Gordura | `fat_free_kg` | decimal; kg | `CDbl(FrmAvaliacaoA.TxtMlg)` (AvaliacaoA.bas:220) | `CDbl(FrmAvaliacaoB.TxtMlg)` (AvaliacaoB.bas:172) | `CDbl(FrmAvaliacaoC.TxtMassaLivreGordura)` (AvaliacaoC.bas:208) |
| AO / 40 | % Musculo Esquelético | `skeletal_muscle_pct` | decimal; % | — | `CDbl(FrmAvaliacaoB.TxtPorcentagemMuscEsqueceletico)` (AvaliacaoB.bas:179) | `CDbl(FrmAvaliacaoC.TxtPorcentagemMuscEsqueceletico)` (AvaliacaoC.bas:215) |
| AP / 41 | Peso Muscular Esquelético | `skeletal_muscle_kg` | decimal; kg | — | `CDbl(FrmAvaliacaoB.TxtPesoMuscular)` (AvaliacaoB.bas:185) | `CDbl(FrmAvaliacaoC.TxtPesoMuscularEsq)` (AvaliacaoC.bas:222) |
| AQ / 42 | Classificação Musc. Esq. | `skeletal_muscle_classification` | texto | — | `FrmAvaliacaoB.TxtPorcentagemMuscEsqueceleticoCLASSIFICACAO` (AvaliacaoB.bas:190) | `FrmAvaliacaoC.TxtPorcentagemMuscEsqueceleticoCLASSIFICACAO` (AvaliacaoC.bas:227) |
| AR / 43 | Nivel Gordura Visceral | `visceral_fat_level` | decimal; razão/escala | — | `CDbl(FrmAvaliacaoB.TxtNivelGorduraVisceral)` (AvaliacaoB.bas:193) | `CDbl(FrmAvaliacaoC.TxtNivelGorduraVisceral)` (AvaliacaoC.bas:230) |
| AS / 44 | Nivel Gordura Visceral Classsificação | `visceral_fat_classification` | texto | — | `FrmAvaliacaoB.TxtNivelGorduraVisceralCLASSIFICACAO` (AvaliacaoB.bas:198) | `FrmAvaliacaoC.TxtNivelGorduraVisceralCLASSIFICACAO` (AvaliacaoC.bas:235) |
| AT / 45 | %Gordura Subcutanea | `subcutaneous_fat_pct` | decimal; % | — | — | `CDbl(FrmAvaliacaoC.TxtGorduraSubcutanea)` (AvaliacaoC.bas:239) |
| AU / 46 | Classificação Subcutanea | `subcutaneous_fat_classification` | texto | — | — | `FrmAvaliacaoC.TxtGorduraSubcutaneaClassificacao` (AvaliacaoC.bas:244) |
| AV / 47 | Agua | `water_pct` | decimal; % | — | — | `CDbl(FrmAvaliacaoC.TxtAguaCorporal)` (AvaliacaoC.bas:249) |
| AW / 48 | Classificação Agua | `water_classification` | texto | — | — | `FrmAvaliacaoC.TxtAguaCorporalClassificacao` (AvaliacaoC.bas:254) |
| AX / 49 | Proteina | `protein_pct` | decimal; % | — | — | `CDbl(FrmAvaliacaoC.TxtProteina)` (AvaliacaoC.bas:259) |
| AY / 50 | Classificação Proteina | `protein_classification` | texto | — | — | `FrmAvaliacaoC.TxtProteinaClassificacao` (AvaliacaoC.bas:264) |
| AZ / 51 | %Ossea | `bone_pct` | decimal; % | `CDbl(FrmAvaliacaoA.TextBox45)` (AvaliacaoA.bas:190) | — | `CDbl(FrmAvaliacaoC.TextBox50)` (AvaliacaoC.bas:270) |
| BA / 52 | Peso Osseo | `bone_kg` | decimal; kg | `CDbl(FrmAvaliacaoA.TxtPesoOsseo)` (AvaliacaoA.bas:196) | — | `CDbl(FrmAvaliacaoC.TxtMassaOssea)` (AvaliacaoC.bas:276) |
| BB / 53 | Classificação Ossea | `bone_classification` | texto | — | — | `FrmAvaliacaoC.TxtMassaOsseaClassificacao` (AvaliacaoC.bas:281) |
| BC / 54 | Peso Residual | `residual_kg` | decimal; kg | `CDbl(FrmAvaliacaoA.TxtPesoResidualKg)` (AvaliacaoA.bas:208) | — | — |
| BD / 55 | Idade Corporal | `body_age_years` | decimal; anos | — | `CDbl(FrmAvaliacaoB.TxtIdadeCorpo)` (AvaliacaoB.bas:201) | `CDbl(FrmAvaliacaoC.TxtIdadeCorpo)` (AvaliacaoC.bas:285) |
| BE / 56 | Idade Corporal Classific. | `body_age_classification` | texto | — | — | `FrmAvaliacaoC.TxtIdadeCorporalClassificacao` (AvaliacaoC.bas:290) |
| BF / 57 | RCQ | `waist_hip_ratio` | decimal; razão/escala | `CDbl(FrmAvaliacaoA.TxtRcq)` (AvaliacaoA.bas:226) | `CDbl(FrmAvaliacaoB.TxtRcq)` (AvaliacaoB.bas:207) | `CDbl(FrmAvaliacaoC.TxtRcq)` (AvaliacaoC.bas:294) |
| BG / 58 | Classificacao RCQ | `waist_hip_classification` | texto | `FrmAvaliacaoA.TxtRcqDesc` (AvaliacaoA.bas:231) | `FrmAvaliacaoB.TxtRcqDesc` (AvaliacaoB.bas:212) | `FrmAvaliacaoC.TxtRcqDesc` (AvaliacaoC.bas:299) |
| BH / 59 | %Gordura Alvo | `target_fat_pct` | decimal; % | `CDbl(FrmAvaliacaoA.TxtPGorduraAlvo)` (AvaliacaoA.bas:235) | `CDbl(FrmAvaliacaoB.TxtPGorduraAlvo)` (AvaliacaoB.bas:216) | `CDbl(FrmAvaliacaoC.TxtPGorduraAlvo)` (AvaliacaoC.bas:303) |
| BI / 60 | IMC Alvo | `target_bmi` | decimal; kg/m² | `CDbl(FrmAvaliacaoA.TxtImcAlvo)` (AvaliacaoA.bas:241) | `CDbl(FrmAvaliacaoB.TxtImcAlvo)` (AvaliacaoB.bas:222) | `CDbl(FrmAvaliacaoC.TxtImcAlvo)` (AvaliacaoC.bas:309) |
| BJ / 61 | Peso Alvo | `target_weight_kg` | decimal; kg | `CDbl(FrmAvaliacaoA.TxtPesoAlvoKg)` (AvaliacaoA.bas:248) | `CDbl(FrmAvaliacaoB.TxtPesoAlvoKg)` (AvaliacaoB.bas:229) | `CDbl(FrmAvaliacaoC.TxtPesoAlvoKg)` (AvaliacaoC.bas:316) |
| BK / 62 | Controle Peso | `weight_control_kg` | decimal; kg | `CDbl(FrmAvaliacaoA.TxtControlePesoKg)` (AvaliacaoA.bas:255) | `CDbl(FrmAvaliacaoB.TxtControlePesoKg)` (AvaliacaoB.bas:236) | `CDbl(FrmAvaliacaoC.TxtControlePesoKg)` (AvaliacaoC.bas:323) |
| BL / 63 | TMB | `bmr_kcal_day` | decimal; kcal/dia | `CDbl(FrmAvaliacaoA.TxtGeb)` (AvaliacaoA.bas:262) | `CDbl(FrmAvaliacaoB.TxtGeb)` (AvaliacaoB.bas:243) | `CDbl(FrmAvaliacaoC.TxtGeb)` (AvaliacaoC.bas:330) |
| BM / 64 | TMB Classific. | `bmr_classification` | texto | — | — | `FrmAvaliacaoC.TxtTmbClassificacao` (AvaliacaoC.bas:335) |
| BN / 65 | GET | `expenditure_kcal_day` | decimal; kcal/dia | `CDbl(FrmAvaliacaoA.TxtGet)` (AvaliacaoA.bas:269) | `CDbl(FrmAvaliacaoB.TxtGet)` (AvaliacaoB.bas:250) | `CDbl(FrmAvaliacaoC.TxtGet)` (AvaliacaoC.bas:339) |
| BO / 66 | Intevalo IMC | `bmi_reference` | texto | — | `FrmAvaliacaoB.TxtIntevaloIMC` (AvaliacaoB.bas:255) | — |
| BP / 67 | Intevalo Gordura | `fat_reference` | texto | — | `FrmAvaliacaoB.TxtIntevaloGordura` (AvaliacaoB.bas:256) | — |
| BQ / 68 | Intevalo Musculo Esq | `skeletal_muscle_reference` | texto | — | `FrmAvaliacaoB.TxtIntervaloMusculo` (AvaliacaoB.bas:257) | — |
| BR / 69 | Intevalo Visceral | `visceral_fat_reference` | texto | — | `FrmAvaliacaoB.TxtIntervaloVisceral` (AvaliacaoB.bas:258) | — |
| BS / 70 | Intevalo RCQ | `waist_hip_reference` | texto | — | `FrmAvaliacaoB.TxtIntervaloRCQ` (AvaliacaoB.bas:259) | — |
| BT / 71 | Peso Total Classificação | `total_weight_classification` | texto | — | — | `FrmAvaliacaoC.TxtPesoClassificacao` (AvaliacaoC.bas:344) |
| BU / 72 | Braço Dir % | `fat_arm_right_reference_pct` | decimal; % do padrão (confirmar semântica) | — | — | `CDbl(FrmAvaliacaoC.TxtPorcentagemBracoDireito)` (AvaliacaoC.bas:348) |
| BV / 73 | Braço Dir Kg | `fat_arm_right_kg` | decimal; kg | — | — | `CDbl(FrmAvaliacaoC.TxtPesoBracoDireito)` (AvaliacaoC.bas:354) |
| BW / 74 | Braço Esq % | `fat_arm_left_reference_pct` | decimal; % do padrão (confirmar semântica) | — | — | `CDbl(FrmAvaliacaoC.TxtPorcentagemBracoEsquerdo)` (AvaliacaoC.bas:360) |
| BX / 75 | Braço Esq Kg | `fat_arm_left_kg` | decimal; kg | — | — | `CDbl(FrmAvaliacaoC.TxtPesoBracoEsquerdo)` (AvaliacaoC.bas:366) |
| BY / 76 | Abs  % | `fat_trunk_reference_pct` | decimal; % do padrão (confirmar semântica) | — | — | `CDbl(FrmAvaliacaoC.TxtPorcentagemAbdominal)` (AvaliacaoC.bas:372) |
| BZ / 77 | Abs Kg | `fat_trunk_kg` | decimal; kg | — | — | `CDbl(FrmAvaliacaoC.TxtPesoAbdominal)` (AvaliacaoC.bas:378) |
| CA / 78 | Perna Dir % | `fat_leg_right_reference_pct` | decimal; % do padrão (confirmar semântica) | — | — | `CDbl(FrmAvaliacaoC.TxtPorcentagemPernaDireita)` (AvaliacaoC.bas:384) |
| CB / 79 | Perna Dir Kg | `fat_leg_right_kg` | decimal; kg | — | — | `CDbl(FrmAvaliacaoC.TxtPesoPernaDireita)` (AvaliacaoC.bas:390) |
| CC / 80 | Perna Esq % | `fat_leg_left_reference_pct` | decimal; % do padrão (confirmar semântica) | — | — | `CDbl(FrmAvaliacaoC.TxtPorcentagemPernaEsquerda)` (AvaliacaoC.bas:396) |
| CD / 81 | Perna Esq Kg | `fat_leg_left_kg` | decimal; kg | — | — | `CDbl(FrmAvaliacaoC.TxtPesoPernaEsquerda)` (AvaliacaoC.bas:402) |
| CE / 82 | %Residual | `residual_pct` | decimal; % | `CDbl(FrmAvaliacaoA.TextBox79)` (AvaliacaoA.bas:202) | — | — |
| CF / 83 | Punho | `wrist_diameter_mm` | decimal; mm — confirmar controle | `CDbl(FrmAvaliacaoA.TxtPunho)` (AvaliacaoA.bas:139) | — | — |
| CG / 84 | Femur | `femur_diameter_mm` | decimal; mm — confirmar controle | `CDbl(FrmAvaliacaoA.TxtFemur)` (AvaliacaoA.bas:146) | — | — |
| CH / 85 | Conclusão | `conclusion` | texto | `FrmAvaliacaoA.txtConclusao` (AvaliacaoA.bas:274) | `FrmAvaliacaoB.txtConclusao` (AvaliacaoB.bas:261) | `FrmAvaliacaoC.txtConclusao` (AvaliacaoC.bas:407) |
| CI / 86 | Pontuacao | `body_score` | decimal; razão/escala | — | — | `CDbl(FrmAvaliacaoC.TxtPontuacao)` (AvaliacaoC.bas:410) |
| CJ / 87 | Z20 Braco D | `z20_arm_right_ohm` | decimal; Ω | — | — | `ZNum(FrmAvaliacaoC.TxtZ20BracoD)` (AvaliacaoC.bas:415) |
| CK / 88 | Z20 Braco E | `z20_arm_left_ohm` | decimal; Ω | — | — | `ZNum(FrmAvaliacaoC.TxtZ20BracoE)` (AvaliacaoC.bas:416) |
| CL / 89 | Z20 Tronco | `z20_trunk_ohm` | decimal; Ω | — | — | `ZNum(FrmAvaliacaoC.TxtZ20Tronco)` (AvaliacaoC.bas:417) |
| CM / 90 | Z20 Perna D | `z20_leg_right_ohm` | decimal; Ω | — | — | `ZNum(FrmAvaliacaoC.TxtZ20PernaD)` (AvaliacaoC.bas:418) |
| CN / 91 | Z20 Perna E | `z20_leg_left_ohm` | decimal; Ω | — | — | `ZNum(FrmAvaliacaoC.TxtZ20PernaE)` (AvaliacaoC.bas:419) |
| CO / 92 | Z100 Braco D | `z100_arm_right_ohm` | decimal; Ω | — | — | `ZNum(FrmAvaliacaoC.TxtZ100BracoD)` (AvaliacaoC.bas:420) |
| CP / 93 | Z100 Braco E | `z100_arm_left_ohm` | decimal; Ω | — | — | `ZNum(FrmAvaliacaoC.TxtZ100BracoE)` (AvaliacaoC.bas:421) |
| CQ / 94 | Z100 Tronco | `z100_trunk_ohm` | decimal; Ω | — | — | `ZNum(FrmAvaliacaoC.TxtZ100Tronco)` (AvaliacaoC.bas:422) |
| CR / 95 | Z100 Perna D | `z100_leg_right_ohm` | decimal; Ω | — | — | `ZNum(FrmAvaliacaoC.TxtZ100PernaD)` (AvaliacaoC.bas:423) |
| CS / 96 | Z100 Perna E | `z100_leg_left_ohm` | decimal; Ω | — | — | `ZNum(FrmAvaliacaoC.TxtZ100PernaE)` (AvaliacaoC.bas:424) |
| CT / 97 | Musc Braco D % | `muscle_arm_right_reference_pct` | decimal; % do padrão (confirmar semântica) | — | — | `ZNum(FrmAvaliacaoC.TxtMuscBracoDirPct)` (AvaliacaoC.bas:426) |
| CU / 98 | Musc Braco D Kg | `muscle_arm_right_kg` | decimal; kg | — | — | `ZNum(FrmAvaliacaoC.TxtMuscBracoDirKg)` (AvaliacaoC.bas:427) |
| CV / 99 | Musc Braco E % | `muscle_arm_left_reference_pct` | decimal; % do padrão (confirmar semântica) | — | — | `ZNum(FrmAvaliacaoC.TxtMuscBracoEsqPct)` (AvaliacaoC.bas:428) |
| CW / 100 | Musc Braco E Kg | `muscle_arm_left_kg` | decimal; kg | — | — | `ZNum(FrmAvaliacaoC.TxtMuscBracoEsqKg)` (AvaliacaoC.bas:429) |
| CX / 101 | Musc Tronco % | `muscle_trunk_reference_pct` | decimal; % do padrão (confirmar semântica) | — | — | `ZNum(FrmAvaliacaoC.TxtMuscTroncoPct)` (AvaliacaoC.bas:430) |
| CY / 102 | Musc Tronco Kg | `muscle_trunk_kg` | decimal; kg | — | — | `ZNum(FrmAvaliacaoC.TxtMuscTroncoKg)` (AvaliacaoC.bas:431) |
| CZ / 103 | Musc Perna D % | `muscle_leg_right_reference_pct` | decimal; % do padrão (confirmar semântica) | — | — | `ZNum(FrmAvaliacaoC.TxtMuscPernaDirPct)` (AvaliacaoC.bas:432) |
| DA / 104 | Musc Perna D Kg | `muscle_leg_right_kg` | decimal; kg | — | — | `ZNum(FrmAvaliacaoC.TxtMuscPernaDirKg)` (AvaliacaoC.bas:433) |
| DB / 105 | Musc Perna E % | `muscle_leg_left_reference_pct` | decimal; % do padrão (confirmar semântica) | — | — | `ZNum(FrmAvaliacaoC.TxtMuscPernaEsqPct)` (AvaliacaoC.bas:434) |
| DC / 106 | Musc Perna E Kg | `muscle_leg_left_kg` | decimal; kg | — | — | `ZNum(FrmAvaliacaoC.TxtMuscPernaEsqKg)` (AvaliacaoC.bas:435) |
| DD / 107 | Sem cabeçalho — hora | `measured_time` | time / hora local | — | — | `FrmAvaliacaoC.TxtHora` (AvaliacaoC.bas:31) |

## Anamnese — 48 colunas

Os booleanos anuláveis são proposta de normalização. O legado grava legendas de controles (sim/não) e textos. Regras de sinais vitais e hidratação estão no catálogo.

| Coluna / offset | Cabeçalho atual | Campo proposto | Tipo/unidade | Gravação observada |
|---|---|---|---|---|
| A / 0 | Seq. | `legacy_id` | inteiro | `CInt(FrmAnamnese.TxtCodA)` (Anamnese.bas:4) |
| B / 1 | Data | `anamnesis_date` | date | `CDate(FrmAnamnese.TxtData)` (Anamnese.bas:5) |
| C / 2 | Id | `patient_legacy_id` | inteiro | `CInt(FrmAnamnese.TxtCodC)` (Anamnese.bas:6) |
| D / 3 | Nome | `patient_name` | texto | `FrmAnamnese.txtNome` (Anamnese.bas:7) |
| E / 4 | Sexo | `sex_for_reference` | texto | `FrmAnamnese.TxtSexo` (Anamnese.bas:8) |
| F / 5 | Idade | `age_years` | inteiro | `CInt(FrmAnamnese.TxtIdade)` (Anamnese.bas:9) |
| G / 6 | F.C REP | `resting_heart_rate` | decimal; bpm | `CDbl(FrmAnamnese.TxtFcREP)` (Anamnese.bas:13) |
| H / 7 | F.C MAX | `maximum_heart_rate` | decimal; bpm | `CDbl(FrmAnamnese.TxtFcMAX)` (Anamnese.bas:20) |
| I / 8 | F.C RES | `reserve_heart_rate` | decimal; bpm | `CDbl(FrmAnamnese.TxtFcRES)` (Anamnese.bas:27) |
| J / 9 | P.A(S) mmHg | `systolic_pressure` | decimal; mmHg | `CDbl(FrmAnamnese.TxtPAS)` (Anamnese.bas:34) |
| K / 10 | P.A(D) mmHg | `diastolic_pressure` | decimal; mmHg | `CDbl(FrmAnamnese.TxtPAD)` (Anamnese.bas:41) |
| L / 11 | P.A Classificação | `pressure_classification` | texto | `FrmAnamnese.TxtPACls` (Anamnese.bas:46) |
| M / 12 | Hidrat. Escala | `hydration_scale` | decimal; razão/escala | `CDbl(FrmAnamnese.TextBox38)` (Anamnese.bas:49) |
| N / 13 | Hidrat. Classif. | `hydration_classification` | texto | `FrmAnamnese.TextBox37` (Anamnese.bas:55) |
| O / 14 | Observações | `observations` | texto | `FrmAnamnese.TxtObs` (Anamnese.bas:59) |
| P / 15 | Bebida Alc | `alcohol` | booleano anulável (proposta) | `FrmAnamnese.OptionButton25.Caption` (Anamnese.bas:65) |
| Q / 16 | Fumante | `smoking` | booleano anulável (proposta) | `FrmAnamnese.OptionButton27.Caption` (Anamnese.bas:74) |
| R / 17 | Bom Sono | `good_sleep` | booleano anulável (proposta) | `FrmAnamnese.OptionButton9.Caption` (Anamnese.bas:82) |
| S / 18 | Sono | `sleep_details` | texto | `FrmAnamnese.TextBox2` (Anamnese.bas:87) |
| T / 19 | Atividade Fisica | `physical_activity` | booleano anulável (proposta) | `FrmAnamnese.OptionButton33.Caption` (Anamnese.bas:91) |
| U / 20 | Atividades | `activity_details` | texto | `FrmAnamnese.TextBox14` (Anamnese.bas:96) |
| V / 21 | Agua Adequada | `adequate_water` | booleano anulável (proposta) | `FrmAnamnese.OptionButton5.Caption` (Anamnese.bas:100) |
| W / 22 | Agua Ideal | `ideal_water_ml` | decimal; mL | `CDbl(FrmAnamnese.TextBox11)` (Anamnese.bas:106) |
| X / 23 | Agua Desc. | `water_details` | texto | `FrmAnamnese.TxtAgua` (Anamnese.bas:111) |
| Y / 24 | Suplementos | `supplements` | booleano anulável (proposta) | `FrmAnamnese.OptionButton7.Caption` (Anamnese.bas:115) |
| Z / 25 | Suplementos Desc. | `supplement_details` | texto | `FrmAnamnese.TextBox1` (Anamnese.bas:120) |
| AA / 26 | Habitos Alimentares | `eating_habits` | texto | `FrmAnamnese.TextBox13` (Anamnese.bas:123) |
| AB / 27 | Anemia | `anemia` | booleano anulável (proposta) | `FrmAnamnese.OptionButton23.Caption` (Anamnese.bas:127) |
| AC / 28 | Diabetes | `diabetes` | booleano anulável (proposta) | `FrmAnamnese.OptionButton61.Caption` (Anamnese.bas:135) |
| AD / 29 | Cardiacos | `cardiac_conditions` | booleano anulável (proposta) | `FrmAnamnese.OptionButton63.Caption` (Anamnese.bas:143) |
| AE / 30 | Hipo/Hipertensao | `blood_pressure_conditions` | booleano anulável (proposta) | `FrmAnamnese.OptionButton53.Caption` (Anamnese.bas:151) |
| AF / 31 | Circulatorios | `circulatory_conditions` | booleano anulável (proposta) | `FrmAnamnese.OptionButton55.Caption` (Anamnese.bas:159) |
| AG / 32 | Alergias | `allergies` | booleano anulável (proposta) | `FrmAnamnese.OptionButton49.Caption` (Anamnese.bas:168) |
| AH / 33 | Outras Doenças | `other_conditions` | texto | `FrmAnamnese.TextBox33` (Anamnese.bas:175) |
| AI / 34 | Tratamento | `treatment` | booleano anulável (proposta) | `FrmAnamnese.OptionButton39.Caption` (Anamnese.bas:180) |
| AJ / 35 | Tratamentos Desc. | `treatment_details` | texto | `FrmAnamnese.TextBox18` (Anamnese.bas:185) |
| AK / 36 | Medicamento | `medications` | booleano anulável (proposta) | `FrmAnamnese.OptionButton71.Caption` (Anamnese.bas:191) |
| AL / 37 | Medicamentos Desc. | `medication_details` | texto | `FrmAnamnese.TextBox35` (Anamnese.bas:196) |
| AM / 38 | Dispositivo | `device` | booleano anulável (proposta) | `FrmAnamnese.OptionButton69.Caption` (Anamnese.bas:201) |
| AN / 39 | Dispositivo Desc. | `device_details` | texto | `FrmAnamnese.TextBox34` (Anamnese.bas:206) |
| AO / 40 | Ansiedade | `anxiety` | booleano anulável (proposta) | `FrmAnamnese.OptionButton75.Caption` (Anamnese.bas:211) |
| AP / 41 | Depressao | `depression` | booleano anulável (proposta) | `FrmAnamnese.OptionButton77.Caption` (Anamnese.bas:219) |
| AQ / 42 | Alimentares | `eating_disorders` | booleano anulável (proposta) | `FrmAnamnese.OptionButton85.Caption` (Anamnese.bas:228) |
| AR / 43 | Bipolar | `bipolar_disorder` | booleano anulável (proposta) | `FrmAnamnese.OptionButton83.Caption` (Anamnese.bas:236) |
| AS / 44 | TOC | `ocd` | booleano anulável (proposta) | `FrmAnamnese.OptionButton79.Caption` (Anamnese.bas:245) |
| AT / 45 | Pós Traum. | `post_traumatic_stress` | booleano anulável (proposta) | `FrmAnamnese.OptionButton87.Caption` (Anamnese.bas:253) |
| AU / 46 | Outros Transt. | `other_mental_conditions` | booleano anulável (proposta) | `FrmAnamnese.OptionButton81.Caption` (Anamnese.bas:261) |
| AV / 47 | Transtornos | `mental_conditions_details` | texto | `FrmAnamnese.TextBox36` (Anamnese.bas:266) |

## Meu Cadastro — inventário de 43 cabeçalhos

Refere-se ao profissional/empresa. Não está incluído na importação de pacientes. Identidade, dados profissionais e dados usados nos quatro documentos devem ser especificados; a presença de um campo legado não aprova sua obrigatoriedade na aplicação.

| Coluna | Cabeçalho atual |
|---|---|
| A | Seq. |
| B | Nome |
| C | Sexo |
| D | Nascimento |
| E | Idade |
| F | Profissao |
| G | Foto Anexo |
| H | Nome Empresarial |
| I | Inscrição Estadual |
| J | Inscrição Municipal |
| K | Data Abertura |
| L | Capital Social |
| M | Situação Atual |
| N | Situação Vigente |
| O | Data Início |
| P | Data Fim |
| Q | Data Cadastro |
| R | Periodo |
| S | Atividade Principal |
| T | Atividade Secundaria |
| U | Telefone |
| V | Celular |
| W | Email |
| X | Observações Gerais |
| Y | Cep |
| Z | Logradouro |
| AA | Numero |
| AB | Complemento |
| AC | Bairro |
| AD | Município |
| AE | UF |
| AF | Rg |
| AG | Rg Anexo |
| AH | Cpf |
| AI | Cpf Anexo |
| AJ | Cnpj |
| AK | Cnpj Anexo |
| AL | Outro |
| AM | Outro Anexo |
| AN | Tipo |
| AO | Orgao |
| AP | Registro |
| AQ | Registro Anexo |

## Dados necessários na web que não possuem coluna equivalente

Propostos: identificador interno, vínculo do usuário/profissional responsável, status rascunho/finalizado, versão da avaliação, entradas originais e origem manual/OCR/cálculo por campo, método de TMB, fator de atividade, versão das regras, versão do template, documento original de OCR, data/autor da revisão, justificativa de correção e evento de auditoria.

Armazenamento de anexos: chave privada e metadados; não reutilizar caminhos absolutos de Windows nem nomes de pacientes como mecanismo de autorização. Datas civis e horas da medição são distintas dos instantes de auditoria.

## Pontos de atenção

- O salvamento atual usa identificadores VBA `Integer`/`CInt`; não herdar a limitação de tamanho no banco web.
- O código de cadastro transforma alguns documentos/endereço em números quando `IsNumeric`; preservar o valor de origem e revisar zeros à esquerda na importação.
- Mês, ano, faixa etária e duplicações de nome/sexo/idade não exigem colunas idênticas no novo banco; manter as informações necessárias para reproduzir a revisão histórica.
- A ausência de gravação em determinado protocolo deve produzir “não informado/não aplicável”, sem herdar resíduos de outro protocolo.
- Há diferença entre cabeçalho, comentário e unidade efetiva: conferir expressões e leitores; os nomes `AguaKg`/`ProteinaKg` do modelo C# são valores já convertidos, não a unidade das células AV/AX.
