public enum GameState
{
    Hangar,     // 격납고
    Loading,    // 씬 전환 및 초기화
    Battle,     // 출격 중
    Paused,     // 일시정지
    Result      // 출격 결과
}

public enum SortieResult
{
    Success,    // 탈출 성공
    Destroyed,  // 플레이어 사망
    Victory,    // 보스 처치 및 승리
    Abandoned   // 출격 포기
}