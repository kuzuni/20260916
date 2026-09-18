namespace Moonlit.UI
{
    /// <summary>Shared authored skill identity. Gameplay values continue to derive from EquipmentRules.</summary>
    public static class SkillCatalog
    {
        static readonly string[,] names = {
            { "치킨 만찬", "뼈 회오리", "돌팔매" },
            { "회복의 사과", "다섯 화살", "중세검 일격" },
            { "연금술 영약", "화승총 사격", "화약 포격" },
            { "전장의 보급", "전차 돌격", "고폭탄 투하" },
            { "생명 유지장", "궤도 레이저", "운석 충돌" },
            { "성운의 숨결", "플라스마 창", "초신성 폭발" },
            { "평행세계의 선물", "차원 절단", "우주 붕괴" },
            { "확률 재생", "양자 관통", "입자 소멸" },
            { "망자의 계약", "영혼 사슬", "명계의 심판" },
            { "신의 축복", "천상의 창", "신의 분노" }
        };
        static readonly string[,] descriptions = {
            { "머리 위의 치킨이 세 번 커졌다 작아진 뒤 사라지고, 몸에 초록 오라가 피어나 체력을 회복합니다.", "주위를 둘러싼 뼈 여덟 개가 원을 그리며 돌다가 차례로 날아가 적의 머리를 휘둘러 때립니다.", "돌 한 개를 포물선으로 던져 적에게 부딪히면 돌조각이 사방으로 흩어집니다." },
            { "머리 위의 사과가 세 번 커졌다 작아진 뒤 사라지고, 몸에 초록 오라가 피어나 체력을 회복합니다.", "화살 다섯 발이 차례로 곡선을 그리며 날아가 적을 맞힙니다.", "중세검이 나타나 적을 향해 한 번 크게 휘둘러지고 검기가 뒤따릅니다." },
            { "영약을 흔들어 기포를 일으킨 뒤 기울여 생명과 힘을 되찾습니다.", "화승총을 뒤로 젖힌 반동으로 불규칙한 세 발을 쏩니다.", "짧게 뜬 포탄의 도화선이 떨리다 다섯 겹 폭발로 퍼집니다." },
            { "보급품이 급강하해 튀어 오른 뒤 회복의 힘을 펼칩니다.", "전차가 궤도를 흔들며 가속해 세 번 들이받습니다.", "흔들리며 멈춘 고폭탄이 급강하해 다섯 번 연쇄 폭발합니다." },
            { "생명 유지 장치가 주위를 돌며 네 방향 보호막을 닫습니다.", "조준광을 모은 뒤 궤도에서 세 번 레이저를 내려칩니다.", "대각선 운석이 가속하며 다섯 번 충돌해 파편을 뿜습니다." },
            { "성운이 나선으로 피어올라 생명의 별빛을 모읍니다.", "회전하는 플라스마 창 세 개가 궤도를 좁히며 관통합니다.", "별이 응축됐다 부풀며 다섯 번 초신성 파동을 방출합니다." },
            { "두 세계의 선물이 서로 자리를 바꾸며 생명과 힘을 건넵니다.", "빈 공간에서 나타난 세 갈래 차원 날이 엇갈려 벱니다.", "나선으로 수축하는 우주가 다섯 차례 적을 중심으로 끌어당깁니다." },
            { "생존 확률의 조각들이 격자 사이를 점멸하며 생명을 복원합니다.", "양자 입자가 세 번 위상을 바꾸며 불연속 궤도로 관통합니다.", "서로 마주한 입자가 멈췄다 충돌하며 다섯 번 소멸합니다." },
            { "땅속 영혼이 흔들리며 올라와 계약자의 생명과 힘으로 스며듭니다.", "구불거리는 사슬이 세 번 휘감아 적의 영혼을 후려칩니다.", "명계의 불기둥 다섯 개가 땅속에서 솟아 적을 심판합니다." },
            { "성광이 하늘에서 빠르게 내려와 신성한 축복으로 정착합니다.", "황금 창 세 개가 높은 정점에서 멈췄다 급강하합니다.", "하늘에 머문 천둥이 고르지 않은 다섯 박자로 내리칩니다." }
        };
        public static string Name(int tier, int variant) => names[tier, variant];
        public static string Description(int tier, int variant) => descriptions[tier, variant];
        public static string IconKey(int tier, int variant)
        {
            if(tier==0 && variant==1)return "Moonlit/Combat/Skills/Focused/Bone-v1";
            if(tier==0 && variant==2)return "Moonlit/Combat/Skills/Tier00/Weak";
            if(tier==1)return "Moonlit/Combat/Skills/Focused/"+new[]{"Apple","Arrow","Sword"}[variant]+"-v1";
            return "Moonlit/Combat/Skills/Tier"+tier.ToString("D2")+"/"+new[]{"Buff","Weak","Strong"}[variant];
        }
    }
}
