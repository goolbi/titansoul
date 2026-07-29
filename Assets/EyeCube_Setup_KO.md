# EyeCube 보스 제작 가이드

## 이미지 역할

| 원본 파일 | 권장 용도 |
|---|---|
| `1784481112115.png` | 보스 방 배경. `Sprite Mode: Single` |
| `1784965112082.png` | 플레이어 또는 전투 이펙트 시트로 추정. 보스와 분리 보관 |
| `1784965111120.png` | 플레이어 또는 전투 이펙트 시트로 추정. 보스와 분리 보관 |
| `1784481752740.png` | EyeCube 레이저 공격 프레임 |
| `1784481789120.png` | EyeCube 몸체 변형/눈 개방 프레임 |
| `1784481751405.png` | EyeCube 눈 감김·개방·이동 프레임 |
| `1784481750314.png` | EyeCube 눈 개방·피격·복귀 프레임 |

시트의 프레임 간 여백과 크기가 일정하지 않으므로 `Grid by Cell Size` 자동 슬라이스보다
Sprite Editor의 수동 사각형 슬라이스가 안전하다. 검은색 또는 투명 여백은 프레임에 포함하지 않는다.

## 1. PNG 가져오기

1. PNG를 `Assets/Art/EyeCube/`에 복사한다.
2. 보스 시트 4개의 Import Settings:
   - Texture Type: `Sprite (2D and UI)`
   - Sprite Mode: `Multiple`
   - Pixels Per Unit: 모든 시트에 같은 값 사용(권장 100)
   - Mesh Type: `Full Rect`
   - Filter Mode: 픽셀 느낌이면 `Point`, 부드럽게 보이게 하려면 `Bilinear`
   - Compression: `None`
3. Sprite Editor에서 보스 중심점(Pivot)을 모든 프레임에서 동일하게 맞춘다.
   권장 기준은 몸체 하단 중앙 `(0.5, 0)`이다.
4. 레이저가 지나치게 큰 프레임은 몸체와 빔을 별도 SpriteRenderer로 분리한다.

## 2. 애니메이션 클립

`Assets/Animations/EyeCube/` 폴더에 다음 클립을 만든다.

| 클립 | Loop | 권장 FPS | 내용 |
|---|---:|---:|---|
| `EyeCube_Sleep` | On | 4 | 눈이 완전히 감긴 프레임 |
| `EyeCube_Open` | Off | 10 | 감김 → 반개방 → 완전 개방 |
| `EyeCube_Move` | On | 8 | 몸체가 좌우/상하로 흔들리는 프레임 |
| `EyeCube_Shoot` | Off | 12 | 눈이 열리고 에너지가 모이는 프레임 |
| `EyeCube_Laser` | Off | 12 | 흰색/분홍색 빔이 번갈아 나오는 프레임 |
| `EyeCube_Hurt` | Off | 14 | 눌리거나 눈이 찌그러지는 프레임 |
| `EyeCube_Dead` | Off | 10 | 눈이 닫히며 몸체가 내려가는 프레임 |

프레임 선택 후 Hierarchy로 드래그하면 Unity가 Animation Clip을 자동 생성한다.
샘플 속도는 Animation 창 우측 상단의 `Samples`에서 조정한다.

## 3. Animator Controller

Animator 파라미터:

- `State` (int)
- `Speed` (float)
- `Hurt` (trigger)
- `Dead` (trigger)

상태 전환:

- Any State → `EyeCube_Hurt`: `Hurt`, Has Exit Time 끔
- Any State → `EyeCube_Dead`: `Dead`, Has Exit Time 끔
- `State == 0` → Sleep
- `State == 1` → Open
- `State == 2` → Move
- `State == 3` → Shoot
- `State == 4` → Laser

State 기반 전환은 Has Exit Time을 끄고 Transition Duration을 `0`으로 둔다.
Hurt가 끝난 뒤에는 Exit Time으로 현재 `State`에 맞는 상태로 복귀시킨다.

## 4. 보스 Prefab 계층

```text
EyeCubeBoss
├── Visual                 SpriteRenderer + Animator
├── EyeMuzzle              눈 중앙 위치
└── Laser                  LineRenderer + EyeCubeLaser
```

루트 `EyeCubeBoss`:

- Rigidbody2D: Body Type `Kinematic`, Gravity Scale `0`
- Collider2D: 몸체보다 약간 작게
- Health: Max Health `100`, Invulnerability `0.08`
- EyeCubeBoss

`Health`의 On Damaged에 `EyeCubeBoss.NotifyHurt`, On Died에
`EyeCubeBoss.NotifyDeath`를 연결할 필요는 없다. 스크립트가 HealthChanged 이벤트로 자동 처리한다.

## 5. 탄환 Prefab

1. SpriteRenderer, Rigidbody2D, CircleCollider2D, `EyeCubeProjectile`을 추가한다.
2. Rigidbody2D는 Body Type `Dynamic`, Gravity Scale `0`, Collision Detection `Continuous`.
3. Collider는 `Is Trigger`를 켠다.
4. `Target Layers`에는 Player 레이어만 선택한다.
5. EyeCubeBoss의 `Projectile Prefab`과 `Eye Muzzle`을 연결한다.

## 6. 레이저

1. `Laser` 자식에 LineRenderer와 `EyeCubeLaser`를 추가한다.
2. LineRenderer Material은 `Sprites/Default` 또는 2D Unlit 재질을 쓴다.
3. Sorting Layer를 보스와 플레이어보다 앞으로 둔다.
4. Charging Color는 흐린 분홍, Firing Color는 흰색 중심의 진한 분홍으로 설정한다.
5. Obstacle Layers에는 벽, Target Layers에는 Player를 설정한다.

원본 레이저 프레임을 그대로 쓰고 싶다면 LineRenderer 아래에 별도 SpriteRenderer를 두고
`EyeCube_Laser` 클립으로 재생한다. 실제 판정은 EyeCubeLaser의 CircleCast가 담당하게 두면
보이는 프레임과 충돌 로직을 안전하게 분리할 수 있다.

## 7. 플레이어 연결 및 실행

1. 플레이어 루트 태그를 `Player`로 설정한다.
2. 플레이어 루트에 `Health`와 Collider2D를 추가한다.
3. Player 레이어를 만들고 플레이어와 EyeCubeProjectile/EyeCubeLaser의 Target Layers를 맞춘다.
4. EyeCubeBoss의 Arena Min/Max를 보스 방 내부 좌표로 설정한다.
5. Play하면 눈을 연 뒤 이동 → 조준탄+방사탄 → 추적 예고 레이저를 반복한다.
6. 체력이 절반 이하가 되면 이동과 공격 템포, 투사체 수, 레이저 지속 시간이 강화된다.

## 튜닝 시작값

- 보스 HP: 100
- Move Speed: 2.2
- Preferred Distance: 5
- Radial Projectile Count: 12
- Laser Charge: 1.25초
- Laser Fire: 0.8초
- Rest Between Actions: 0.65초

레이저는 발사 전에 얇은 예고선을 보여 주므로 피할 수 있다. 보스 공격에서 가장 중요한 것은
피해량보다도 예고 동작과 실제 판정 시점을 명확히 분리하는 것이다.
