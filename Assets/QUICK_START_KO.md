# TitanSoul 초간단 설정

## 조작

- 이동: `WASD` 또는 방향키
- 조준: 마우스
- 공격: 마우스 왼쪽
- 대시: `Space` 또는 왼쪽 `Shift`

## 1. 플레이어 만들기

Hierarchy에서 빈 오브젝트를 만들고 이름을 `Player`로 바꾼다.

붙일 것:

1. `SpriteRenderer`
2. `Rigidbody2D`
3. `CircleCollider2D`
4. `Health`
5. `PlayerController`

설정:

- Tag: `Player`
- Layer: 새로 만든 `Player`
- Rigidbody2D의 Gravity Scale: `0`
- Health의 Max Health: `10`

Player 아래에 빈 자식 `FirePoint`를 만들고 캐릭터 앞쪽에 둔다.
`PlayerController`의 Fire Point에 연결한다.

## 2. 플레이어 총알 만들기

빈 오브젝트 `PlayerBullet`을 만든다.

붙일 것:

1. `SpriteRenderer`
2. `Rigidbody2D`
3. `CircleCollider2D` (`Is Trigger` 켜기)
4. `PlayerProjectile`

`PlayerProjectile`의 Target Layers에는 EyeCube가 사용할 `Enemy` 레이어를 선택한다.
오브젝트를 Project 창으로 끌어 Prefab으로 만든 뒤 PlayerController의 Projectile Prefab에 연결한다.

## 3. EyeCube 만들기

빈 오브젝트 `EyeCubeBoss`를 만든다.

붙일 것:

1. `SpriteRenderer`
2. `Animator`
3. `Rigidbody2D`
4. `BoxCollider2D`
5. `Health`
6. `EyeCubeBoss`

설정:

- Layer: `Enemy`
- Rigidbody2D Body Type: `Kinematic`
- Health Max Health: `100`

EyeCube 아래에 `EyeMuzzle`을 만들고 눈 중앙에 둔다.

## 4. EyeCube 총알

플레이어 총알과 같은 방법으로 만들되 `EyeCubeProjectile`을 붙인다.
Target Layers에는 `Player`를 선택한다.

EyeCubeBoss의 다음 칸을 연결한다.

- Target: Player
- Eye Muzzle: EyeMuzzle
- Projectile Prefab: EyeCube 총알 Prefab

## 5. 애니메이션

애니메이션과 Animator Controller는 프로젝트가 자동 생성한다.

Player의 `Visual` 오브젝트를 선택하고 Animator의 Controller에 다음 파일을 넣는다.

`Assets/Animations/Generated/Player/PlayerAnimator.controller`

EyeCube의 Animator에는 다음 파일을 넣는다.

`Assets/Animations/Generated/EyeCube/EyeCubeAnimator.controller`

끝이다. 다시 만들고 싶으면 Unity 상단 메뉴에서 다음을 누른다.

`TitanSoul → Animation → Rebuild Player and EyeCube`

생성된 플레이어 이미지는 다음 위치에 있다.

`Assets/Art/Player/Generated/PlayerSheet.png`

더 자세한 EyeCube 레이저 설정은 `EyeCube_Setup_KO.md`를 참고한다.
