using System;
using System.Collections.Generic;

namespace SistemaPSM.AddIn.Laudo
{
    /// <summary>
    /// Snapshot dos dados de uma linha da aba "Avaliações" usados no laudo.
    /// Campos de bioimpedância (Músculo Esquelético, Visceral, Idade Corporal,
    /// Massa Livre de Gordura) só vêm preenchidos em avaliações desse tipo.
    /// </summary>
    public class AvaliacaoLaudo
    {
        // Identificação
        public int Id;
        public string Nome;
        public string Sexo;
        public int Idade;
        public double AlturaCm;
        public DateTime? Data;

        // Composição corporal
        public double Peso;                 // J  (offset 9)
        public double Imc;                  // AD (29)
        public double GorduraPct;           // AF (31)
        public double GorduraKg;            // AG (32)
        public double MassaMagraKg;         // AN (39)
        public double MusculoEsqPct;        // AO (40)
        public double MusculoEsqKg;         // AP (41)
        public double GorduraVisceral;      // AR (43)
        public double IdadeCorporal;        // BD (55)
        public double Rcq;                  // BF (57)
        public double Tmb;                  // BL (63)
        public double Get;                  // BN (65)

        // Classificações (texto)
        public string ImcClassif;           // AE (30)
        public string GorduraClassif;       // AH (33)
        public string MusculoClassif;       // AQ (42)
        public string VisceralClassif;      // AS (44)
        public string RcqClassif;           // BG (58)

        // Intervalos de referência / faixa normal (texto, ex.: "18,5 - 24,9", "<= 9")
        public string ImcIntervalo;         // BP (66)
        public string GorduraIntervalo;     // BQ (67)
        public string MusculoIntervalo;     // BR (68)
        public string VisceralIntervalo;    // BS (69)
        public string RcqIntervalo;         // BT (70)

        // Circunferências (cm)
        public double CircPeitoral;         // R (17)  -> "Peito"
        public double CircAbdomen;          // W (22)  -> "Barriga"
        public double CircQuadril;          // Y (24)  -> "Quadril"

        // ---- Campos extras (laudo de avaliação única, estilo Relaxmedic) ----
        // Preenchidos só por LaudoRepositorio.LerCompletaPorSeq. Inertes no laudo de evolução.
        public double TaxaMuscular;         // AI (34) % músculo (massa muscular total)
        public string TaxaMuscularClassif;  // AJ (35)
        public double PesoMuscular;         // AK (36) kg músculo (massa muscular total)
        public string PesoMuscularClassif;  // AL (37)
        public double MassaLivrePct;        // AM (38) % massa livre de gordura
        public double GorduraSubcutaneaPct; // AT (45)
        public string SubcutaneaClassif;    // AU (46)
        public double AguaKg;               // AV (47)
        public string AguaClassif;          // AW (48)
        public double ProteinaKg;           // AX (49)
        public string ProteinaClassif;      // AY (50)
        public double OsseaPct;             // AZ (51)
        public double PesoOsseo;            // BA (52) "Sal inorgânico"/mineral ósseo
        public string OsseaClassif;         // BB (53)
        public double PesoResidual;         // BC (54)
        public double ResidualPct;          // CE (82)
        public string IdadeCorporalClassif; // BE (56)
        public double GorduraAlvoPct;       // BH (59)
        public double ImcAlvo;              // BI (60)
        public double PesoAlvo;             // BJ (61)
        public double ControlePeso;         // BK (62)
        public string TmbClassif;           // BM (64)
        public string PesoTotalClassif;     // BT (71)

        // Pontuação corporal (0–100) — digitada no FrmAvaliacaoC, coluna CI (86).
        public double Pontuacao;            // CI (86)

        // Hora da medição (texto "HH:mm") — digitada no FrmAvaliacaoC, coluna DD (107).
        public string Hora;                 // DD (107)

        // Impedância bioelétrica Z (Ω) — digitada no FrmAvaliacaoC, colunas CJ–CS (87–96).
        public double Z20BracoD, Z20BracoE, Z20Tronco, Z20PernaD, Z20PernaE;        // CJ–CN (87–91)
        public double Z100BracoD, Z100BracoE, Z100Tronco, Z100PernaD, Z100PernaE;   // CO–CS (92–96)

        // Músculo segmentar (% relativo ao padrão + kg) — 2ª silhueta "Equilíbrio muscular".
        // Digitado no FrmBioimpC, colunas CT–DC (97–106). Estrutura espelha o BU–CD (gordura).
        public double MuscBracoDirPct;      // CT (97)
        public double MuscBracoDirKg;       // CU (98)
        public double MuscBracoEsqPct;      // CV (99)
        public double MuscBracoEsqKg;       // CW (100)
        public double MuscAbsPct;           // CX (101)
        public double MuscAbsKg;            // CY (102)
        public double MuscPernaDirPct;      // CZ (103)
        public double MuscPernaDirKg;       // DA (104)
        public double MuscPernaEsqPct;      // DB (105)
        public double MuscPernaEsqKg;       // DC (106)

        // Segmentar (% relativo ao padrão + kg) — Braço D/E, Abdome/Tronco, Perna D/E
        public double BracoDirPct;          // BU (72)
        public double BracoDirKg;           // BV (73)
        public double BracoEsqPct;          // BW (74)
        public double BracoEsqKg;           // BX (75)
        public double AbsPct;               // BY (76)
        public double AbsKg;                // BZ (77)
        public double PernaDirPct;          // CA (78)
        public double PernaDirKg;           // CB (79)
        public double PernaEsqPct;          // CC (80)
        public double PernaEsqKg;           // CD (81)

        /// <summary>
        /// Texto livre da coluna "Conclusão" (CH, offset 85) da avaliação selecionada.
        /// Preenchido só na avaliação "atual" (laudo de evolução); vazio na maioria das linhas.
        /// </summary>
        public string Conclusao;

        /// <summary>
        /// Série histórica para Evolução/Circunferência: a primeira avaliação do paciente +
        /// as últimas (até 6 colunas), em ordem cronológica crescente, incluindo esta (a última).
        /// </summary>
        public List<AvaliacaoLaudo> Serie;

        /// <summary>
        /// SMI (Skeletal Muscle Index) = Peso Muscular Esquelético (kg) / Altura² (m²).
        /// Retorna 0 se não houver dados suficientes.
        /// </summary>
        public double Smi
        {
            get
            {
                if (MusculoEsqKg <= 0 || AlturaCm <= 0) return 0;
                double m = AlturaCm / 100.0;
                return MusculoEsqKg / (m * m);
            }
        }
    }
}
