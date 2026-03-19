# Research Landscape: Gaze × Crowd × Diffusion

## 現在のポジション

```
既存研究:
  SPDiff (AAAI 2024):     群衆状態 → Diffusion → 2D加速度 (視線なし)
  GazeMoDiff (PG 2024):   視線+モーション → Diffusion → 全身モーション (1人、室内)
  GAP3DS (CVPR 2025):     視線+シーン → Diffusion → モーション (1人)
  CrowdES (CVPR 2025):    Diffusion でエージェント配置 (Layer 0 Strategic)

空いている場所 (=研究機会):
  視線 + 群衆状態 + 3Dシーン → Diffusion → 移動軌道 × N人
```

## レイヤー別カバレッジ

### 群衆シミュレーションの伝統的レイヤー

| Layer | 名前 | Diffusion研究 | 状況 |
|-------|------|--------------|------|
| 0 | Strategic (配置) | CrowdES | ✅ |
| 1 | Tactical (経路計画) | DTG, DiffPlanner (自動運転向け) | △ |
| 2 | Operational (局所回避) | SPDiff (2D位置のみ) | △ |
| 3 | Animation (全身動作) | MDM, PDP (単体のみ) | ❌ 群衆は0 |
| 4 | Detail (視線・表情) | DiffEye (2Dのみ) | ❌ 群衆は0 |

### 認知プロセスのレイヤー

| Layer | Diffusion研究 | 状況 |
|-------|--------------|------|
| Perception | ほぼ0 | ❌ |
| Attention | 2Dのみ | ❌ |
| Prediction | 活発 (ただし2D位置) | ✅ |
| Decision | 一部 (CrowdES) | △ |
| Motor | 単体のみ | ❌ 群衆は0 |

**下層 (Motor/Animation/Detail) と認知の上流 (Perception/Attention) が完全に空白。**

## データセット

| データセット | 年 | 内容 | 規模 |
|-------------|-----|------|------|
| **Nymeria** | ECCV 2024 | 視線+全身+3Dシーン+テキスト | **264人、300時間** |
| GIMO | ECCV 2022 | 視線+全身+3Dシーン（室内） | 11人 |
| MoGaze | 2020 | 視線+MoCap（pick&place） | 7人、3時間 |
| EgoCampus | 2024 | 歩行者の視線（屋外） | 82人 |

## GIMO 系譜

```
GIMO (ECCV 2022) ← 原点
  ├── GazeMotion (IROS 2024) ← GCN で予測改良
  ├── GazeMoDiff (PG 2024) ← Diffusion で多様なモーション生成
  ├── Prime and Reach (2025) ← 到達動作の生成
  └── GaMo (2025) ← 新データセット（インタラクション含む）
```

GazeMoDiff の限界: 3Dシーン情報なし、単体のみ、室内タスク動作のみ

## SPDiff の核心

```
Social Force Model を分解:
  a = f_dest(数式のまま) + f_ped(Diffusion で学習) + f_hist(LSTMで学習)

Diffusion がやっていること:
  ノイズから「N人分の2D加速度ベクトル」を生成
  ノイズ除去ネットワーク = EGCL(等変グラフ畳み込み) + LSTM
```

## 認知科学の知見

- 視線は行動に **2〜4歩先行** する
- 見ている方向 ≈ 行く方向
- Microsaccade がないと「死んだ目」

## 提案する研究方向

**Attention → Decision を Diffusion で統合**
- Diffusion の担当: Attention → Decision (どこを見て、どう移動するか)
- Motion Matching: Motor (その移動に合った歩行アニメーション)

**ターゲット:**
視線 + 群衆状態 + 3Dシーン → Diffusion → 移動軌道 × N人
データ: Nymeria (264人、300時間) で十分
