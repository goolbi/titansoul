# TitanSoul 백업 복원 방법

## 필요한 Unity 버전

- Unity `6000.3.10f1`
- Unity Hub를 통해 해당 버전을 설치하는 것을 권장합니다.

## 새 컴퓨터에서 복원

1. 백업 ZIP을 원하는 폴더에 압축 해제합니다.
2. Unity Hub를 실행합니다.
3. `Add project from disk`를 누릅니다.
4. 압축 해제한 `titansoul` 프로젝트 폴더를 선택합니다.
5. Unity가 `Library` 폴더와 패키지를 다시 생성할 때까지 기다립니다.
6. `Assets/Scenes/SampleScene.unity`를 엽니다.

## 정상 복원 확인

- Hierarchy에 `EyeCubeArena`가 표시되는지 확인합니다.
- 플레이어의 `PlayerController`와 `PlayerAnimator` 연결을 확인합니다.
- `Assets/Prefabs/Maps/EyeCubeArena.prefab`이 존재하는지 확인합니다.
- `Assets/Prefabs/Projectiles/Generated/MagicArrow.prefab`이 존재하는지 확인합니다.

## 참고

`Library`, `Temp`, `Logs`, `UserSettings`는 컴퓨터별 캐시이므로 백업에 포함하지 않았습니다.
Unity가 프로젝트를 처음 열 때 자동으로 다시 생성합니다.
