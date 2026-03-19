# Dataset Catalog: 群衆シミュレーション構築に使えるデータセット

> 2026-03-16 作成。全データセットの含有情報を原論文・公式ページから検証済み。

## マスターテーブル

凡例: ✅ = あり, △ = 部分的/限定的, ❌ = なし

| # | Dataset | 年 | 会場 | 2D軌道 | 3D軌道 | 頭部方向 | 視線(Eye) | 全身骨格 | 3Dシーン | グループ | 社会行動 | 規模 | 環境 | Link |
|---|---------|-----|------|:------:|:------:|:--------:|:---------:|:--------:|:--------:|:--------:|:--------:|------|------|------|
| **A. 視線 + 全身 + 3Dシーン（単体）** |
| 1 | **Nymeria** | 2024 | ECCV | — | △ 6DoFデバイス軌道 | △ デバイスpose | △ モデルベース推定 | ✅ カスタム体モデル (XSens 240Hz, 17IMU) | ✅ Point cloud | ❌ | ❌ | 264人, 300h, 260M poses, 1200seq | 屋内外 50箇所 | [Link](https://www.projectaria.com/datasets/nymeria/) |
| 2 | **GIMO** | 2022 | ECCV | — | △ root軌道 | △ SMPL-X head joint | ✅ Pupil Labs | ✅ SMPL-X (Perception Neuron IMU) | ✅ 3D scan | ❌ | ❌ | 複数人, 室内複数シーン | 室内 | [Link](https://geometry.stanford.edu/projects/gimo/) |
| 3 | **GaMo** | 2025 | IEEE VRW | — | △ VR tracking | △ HMD pose | ✅ HMD eye tracker | ✅ MoCap (VR) | △ VR環境 | ❌ | △ inter-subject interaction | VR環境 | VR室内 | [Link](https://ieeexplore.ieee.org/document/10972869/) |
| 4 | **MoGaze** | 2020 | RA-L | — | △ body root | △ head markers | ✅ Pupil Labs 200Hz | ✅ 光学MoCap markers 120Hz | △ workspace geometry (物体位置のみ) | ❌ | ❌ | 7人, 180min, 1627 pick&place | テーブルタスク | [Link](https://humans-to-robots-motion.github.io/mogaze/) |
| **B. 視線 + 軌道（群衆あり）** |
| 5 | **THÖR** | 2020 | RA-L | — | ✅ Qualisys 100Hz | ✅ 3D (MoCap rigid body) | ✅ Tobii Pro Glasses 50Hz | ❌ 頭部のみ | ✅ 3D LiDAR + 障害物マップ | ✅ | ✅ 役割(visitor/worker/inspector) | ~9人, 600+軌道, 60min | 室内(制御, 8.4x18.8m) | [Link](http://thor.oru.se/thor.html) |
| 6 | **THÖR-MAGNI** | 2024 | IJRR | — | ✅ Qualisys 100Hz | ✅ 3D (6DoF head) | ✅ Tobii 50Hz / Pupil 100Hz (16/40人) | ❌ 頭部のみ | ✅ Ouster LiDAR + Azure Kinect + 魚眼カメラ | ✅ 役割ベース | ✅ 5シナリオ, ロボット含む | 40人, 3.5h, 52 runs, 5日間 | 室内(制御) | [Link](http://thor.oru.se/magni.html) |
| 7 | **MuseumVisitors** | 2015 | CVPRW | △ bbox追跡 | — | ✅ 頭部+体の向き | △ 頭部方向推定(カメラ画像から, eye trackingではない) | ❌ | ❌ | ✅ | ✅ 鑑賞対象アノテーション | 美術館来訪者 | 室内(美術館, 3カメラ 5fps) | [Link](https://www.micc.unifi.it/resources/datasets/museumvisitors/) |
| **C. 視線 + 歩行（単体）** |
| 8 | **EgoCampus** | 2024 | arXiv | — | △ GPS(粗い) | △ Aria 6DoF device pose | ✅ Project Aria eye tracker 30Hz | ❌ | ❌ (3D再構築なし) | ❌ | ❌ | 82人, 6km, 25経路, 32h video | 屋外キャンパス | [Link](https://arxiv.org/abs/2512.07668) |
| **D. 2D群衆軌道（定番ベンチマーク）** |
| 9 | **ETH (BIWI)** | 2009 | ICCV | ✅ world座標(m) | — | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ~750軌道, 2シーン (ETH, HOTEL) | 屋外(チューリッヒ), 2.5fps | [Link](https://icu.ee.ethz.ch/research/datsets.html) |
| 10 | **UCY** | 2007 | CGF | ✅ world座標 | — | ✅ head pan angle | ❌ | ❌ | ❌ | ❌ | ❌ | 786軌道, 3シーン (UNIV, ZARA1, ZARA2) | 屋外(キプロス), 2.5fps | [Link](https://graphics.cs.ucy.ac.cy/portfolio) |
| 11 | **SDD** | 2016 | ECCV | ✅ pixel座標(bbox) | — | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ~11,000軌道, 多クラス(歩行者+車+自転車等) | 大学キャンパス(ドローン鳥瞰), 30fps | [Link](https://cvgl.stanford.edu/projects/uav_data/) |
| 12 | **TrajNet++** | 2020 | — | ✅ (統合, 2D位置のみ) | — | ❌ (元データの頭部方向は破棄) | ❌ | ❌ | ❌ | ❌ | ❌ | 統合ベンチマーク(ETH/UCY/SDD/WildTrack等) | 統一2.5fps | [Link](https://github.com/vita-epfl/trajnetplusplusdataset) |
| 13 | **Grand Central** | 2012 | CVPR | ✅ pixel座標(ワールド座標なし) | — | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | 12,684人, 33min, 6001 frames | 室内(NY駅, 監視カメラ), 25fps | [Link](https://www.ee.cuhk.edu.hk/~xgwang/grandcentral.html) |
| 14 | **Lyon Festival** | 2025 | Sci Data | ✅ world座標(RGF-93) | — | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ~7,000軌道, GPS+接触カウント | 屋外イベント(4人/m²), 10Hz | [Link](https://www.nature.com/articles/s41597-025-04732-3) |
| **E. 大規模群衆軌道（ショッピング等）** |
| 15 | **ATC** | 2012 | — | ✅ (x,y,z mm) | ✅ 高さ含む | ✅ facing angle (体の向き, rad) | ❌ | ❌ | ❌ | ❌ (グループラベルなし) | ❌ | 92日間, ~33Hz | 室内(大阪ATC, 900m², 49センサ) | [Link](https://dil.atr.jp/crest2010_HRI/ATC_dataset/) |
| 16 | **ATR Group** | — | — | ✅ (x,y,z mm) | ✅ 高さ含む | ✅ facing angle (体の向き, rad) | ❌ | ❌ | ❌ | ✅ グループID+サイズ+パートナーID | ✅ interaction partner | 8日間, ~33Hz | 室内(DIAMOR+ATC, 大阪) | [Link](https://dil.atr.jp/ISL/sets/groups/) |
| **F. 3D検出 + マルチカメラ/ロボット** |
| 17 | **JRDB** | 2019 | T-PAMI | ✅ | ✅ 3D cuboid | ❌ | ❌ | ❌ | ✅ 360° RGB + 2x LiDAR | ❌ (base) | ❌ (base) | 54seq, 60K frames, 3500+軌道, 2.4M bbox | 大学キャンパス(ロボット視点), 15fps | [Link](https://jrdb.erc.monash.edu/) |
| 17a | **+JRDB-Act** | 2022 | CVPR | (↑) | (↑) | ❌ | ❌ | ❌ | (↑) | ✅ グループID | ✅ 2.8M行動ラベル(26カテゴリ) | (↑ に追加) | (↑) | [Link](https://jrdb.erc.monash.edu/) |
| 17b | **+JRDB-Pose** | 2022 | — | (↑) | (↑) | ✅ 頭部bbox(600K) | ❌ | △ 2Dキーポイント(600K) | (↑) | (↑) | (↑) | (↑ に追加) | (↑) | [Link](https://jrdb.erc.monash.edu/) |
| 17c | **+JRDB-Social** | 2024 | CVPR | (↑) | (↑) | (↑) | ❌ | (↑) | (↑) | ✅ 3階層アノテーション | ✅ 20カテゴリ pairwise interaction, 人口統計 | (↑ に追加) | (↑) | [Link](https://jrdb.erc.monash.edu/) |
| 18 | **STCrowd** | 2022 | CVPR | ✅ | ✅ 3D bbox | ❌ | ❌ | ❌ | ✅ LiDAR(128beam)+ステレオカメラ | ❌ | ❌ | 219K instances, 20人/frame avg | 屋外(混雑), 15Hz | [Link](https://arxiv.org/abs/2204.01026) |
| 19 | **WildTrack** | 2018 | CVPR | ✅ | ✅ ground-plane world座標 | ❌ | ❌ | ❌ | △ マルチカメラ(7台GoPro) | ❌ | ❌ | 313人, avg 20/frame, 400 annotated frames | 屋外(ETH前広場), 2fps annotation | [Link](https://www.epfl.ch/labs/cvlab/data/data-wildtrack/) |
| 20 | **Oxford Town Centre** | 2011 | BMVC | ✅ | — | △ 頭部bbox (向き角なし) | ❌ | ❌ | ❌ | ❌ | ❌ | ~230人, ~5min | 屋外(Oxford商店街), 25fps | [Link](https://academictorrents.com/details/35e83806d9362a57be736f370c821960eb2f2a01) |
| **G. 3D骨格 + 群衆（視線なし）** |
| 21 | **Waymo-3DSkelMo** | 2025 | arXiv | — | ✅ | ❌ | ❌ | ✅ 3D骨格(LiDARから推定, body prior) | ✅ LiDAR | △ interaction semantics | ❌ | avg 27人/scene, max 250, 14,000s, 800+ scenarios | 屋外(自動運転), 10Hz | [Link](https://arxiv.org/abs/2508.09404) |
| **H. 自動運転（歩行者含む）** |
| 22 | **nuScenes** | 2020 | CVPR | — | ✅ 3D bbox | ❌ | ❌ | ❌ | ✅ LiDAR(32beam)+6cam | ❌ | ❌ | 1000 scenes x 20s, 40K keyframes, 1.4M bbox | 屋外(Boston+Singapore), 2Hz annotation | [Link](https://www.nuscenes.org/) |
| 23 | **Argoverse 2** | 2021 | NeurIPS | — | ✅ 3D cuboid | ❌ | ❌ | ❌ | ✅ 2x LiDAR + 9cam + HD vector map | ❌ | △ interaction mining | 250K scenarios x 11s, 6都市 | 屋外(米国6都市), 10Hz | [Link](https://www.argoverse.org/) |
| 24 | **Waymo Open** | 2020 | CVPR | — | ✅ 3D bbox | ❌ | ❌ | ❌ | ✅ 5x LiDAR + 5cam | ❌ | ❌ | 1150 segments x 20s, 200K labeled frames | 屋外(米国多都市), 10Hz | [Link](https://waymo.com/open/) |
| **I. 頭部検出・追跡（群衆）** |
| 25 | **RPEE-Heads** | 2024 | arXiv | ❌ (画像のみ, 追跡なし) | — | ❌ (検出のみ, 向き角なし) | ❌ | ❌ | ❌ | ❌ | ❌ | 109K heads, 1886画像, 66動画 | 鉄道+イベント(高密度) | [Link](https://arxiv.org/abs/2411.18164) |
| 26 | **CcHead** | 2025 | — | △ 頭部追跡による暗黙的軌道 | — | ❌ (追跡のみ, 向き角なし) | ❌ | ❌ | ❌ | ❌ | ❌ | 50K frames, 2.3M heads, 2358 tracks | 中国10シーン(高密度) | [Link](https://arxiv.org/abs/2408.05877) |
| **J. ソーシャルナビゲーション（ロボット）** |
| 27 | **TBD Pedestrian** | 2024 | — | ✅ | △ | ❌ | ❌ | ❌ | △ RGB-D + LiDAR | ❌ | △ social navigation scenarios | ドキュメント限定的 | 大学キャンパス | [Link](https://tbd.ri.cmu.edu/resources/tbd-social-navigation-datasets/) |
| **K. 全身モーション（単体、群衆なし）** |
| 28 | **AMASS** | 2019 | ICCV | — | — | △ skeleton joint chain | ❌ | ✅ SMPL+H (15データセット統合) | ❌ | ❌ | ❌ | 300+人, 11K+ sequences, 40h+ | MoCap studio, 60-120fps | [Link](https://amass.is.tue.mpg.de/) |
| 29 | **BABEL** | 2021 | CVPR | — | — | △ (AMASS継承) | ❌ | ✅ SMPL+H (AMASS継承) | ❌ | ❌ | ❌ | 28K sequence labels, 63K frame labels, 250+カテゴリ, 43h | AMASS上のテキストラベル | [Link](https://babel.is.tue.mpg.de/) |
| 30 | **Motion-X** | 2023 | NeurIPS | — | — | △ (SMPL-X head) | ❌ | ✅ SMPL-X (全身+手+顔) | ❌ | ❌ | ❌ | 81K sequences, 15.6M poses, テキスト付き | 多様(MoCap+動画), 30-120fps | [Link](https://motion-x-dataset.github.io/) |
| 31 | **Motion-X++** | 2025 | arXiv | — | — | △ (SMPL-X head) | ❌ | ✅ SMPL-X + RGB動画 + 音声 | ❌ | ❌ | ❌ | 120K seq, 19.5M poses, 80K videos, 45K audio | 多様 | [Link](https://arxiv.org/abs/2501.05098) |
| **L. 統合ツール（データセットではなくフレームワーク）** |
| 32 | **OpenTraj** | 2020 | ACCV | ✅ | — | — | — | — | — | — | — | 31データセット統合評価フレームワーク | — | [Link](https://github.com/crowdbotp/OpenTraj) |
| 33 | **trajdata** | 2023 | NeurIPS | ✅ | ✅ | — | — | — | — | — | — | 24+データセット統一APIローダー | — | [Link](https://github.com/NVlabs/trajdata) |

---

## 含有データ早見表

```
                   2D軌道  3D軌道  頭部方向  Eye Gaze  全身骨格  3Dシーン  グループ  社会行動
─────────────────────────────────────────────────────────────────────────────────────────
Nymeria              —      △       △ device  △ model    ✅       ✅        ❌       ❌
GIMO                 —      △       △ joint   ✅ Pupil   ✅ SMPLX ✅        ❌       ❌
GaMo                 —      △       △ HMD     ✅ HMD     ✅ VR     △ VR      ❌       △
MoGaze               —      △       △ marker  ✅ Pupil   ✅ optical △ workspace ❌    ❌
THÖR                 —      ✅      ✅ MoCap   ✅ Tobii   ❌       ✅ LiDAR   ✅       ✅
THÖR-MAGNI           —      ✅      ✅ MoCap   ✅ 16/40人  ❌       ✅ LiDAR   ✅       ✅
MuseumVisitors       △      —       ✅ 頭+体   △ 頭方向推定 ❌     ❌        ✅       ✅
EgoCampus            —      △ GPS   △ device  ✅ Aria     ❌       ❌        ❌       ❌
ETH (BIWI)           ✅     —       ❌        ❌         ❌       ❌        ❌       ❌
UCY                  ✅     —       ✅ pan角   ❌         ❌       ❌        ❌       ❌
SDD                  ✅ px  —       ❌        ❌         ❌       ❌        ❌       ❌
Grand Central        ✅ px  —       ❌        ❌         ❌       ❌        ❌       ❌
Lyon Festival        ✅     —       ❌        ❌         ❌       ❌        ❌       ❌
ATC                  ✅     ✅ 高さ  ✅ facing  ❌         ❌       ❌        ❌       ❌
ATR Group            ✅     ✅ 高さ  ✅ facing  ❌         ❌       ❌        ✅       ✅
JRDB (全体)          ✅     ✅      △ +Pose   ❌         △ 2D KP  ✅ LiDAR   ✅ +Act   ✅ +Social
STCrowd              ✅     ✅      ❌        ❌         ❌       ✅ LiDAR   ❌       ❌
WildTrack            ✅     ✅ ground ❌       ❌         ❌       △ multi-cam ❌      ❌
Oxford Town Centre   ✅     —       △ 頭bbox  ❌         ❌       ❌        ❌       ❌
Waymo-3DSkelMo       —      ✅      ❌        ❌         ✅ 推定   ✅ LiDAR   △       ❌
nuScenes             —      ✅      ❌        ❌         ❌       ✅ LiDAR   ❌       ❌
Argoverse 2          —      ✅      ❌        ❌         ❌       ✅ LiDAR   ❌       △
Waymo Open           —      ✅      ❌        ❌         ❌       ✅ LiDAR   ❌       ❌
AMASS                —      —       △ joint   ❌         ✅ SMPL+H ❌       ❌       ❌
Motion-X             —      —       △ joint   ❌         ✅ SMPLX  ❌       ❌       ❌
─────────────────────────────────────────────────────────────────────────────────────────
```

---

## 検証で見つかった主な修正点

| # | Dataset | 修正内容 |
|---|---------|---------|
| 1 | Nymeria | 体モデルはSMPLではなく**カスタム parametric model** (XSens)。視線は生のpupil trackingではなく**モデルベース推定** |
| 2 | GIMO | 体モデルは**SMPL-X**。MoCapは**Perception Neuron (IMU)**。視線は**Pupil Labs** |
| 3 | GaMo | **VRベース**のデータセット (HMD+コントローラー)。実世界MoCapではない |
| 4 | MoGaze | 視線は**Pupil Labs 200Hz**。MoCapは光学マーカー**120Hz**。3Dシーンはworkspace geometry(物体位置)のみで全環境スキャンではない |
| 5 | THÖR | 視線は**Tobii Pro Glasses 50Hz**。**全身骨格なし**(頭部位置・方向のみ)。3D LiDARあり |
| 6 | THÖR-MAGNI | eye trackerは**16/40人のみ装着**。Tobii 50Hz + Pupil 100Hz。**全身骨格なし** |
| 7 | MuseumVisitors | 「gaze」は**頭部方向のカメラ推定**であり、eye trackingではない |
| 8 | EgoCampus | **3Dシーン再構築なし**。3D軌道はGPS(粗い)のみ |
| 9 | ETH | ICCV **2009** (2007ではない)。Pellegrini et al. |
| 10 | UCY | **頭部pan angle アノテーションあり** (見落とされがち) |
| 11 | SDD | 軌道は**pixel座標**(bbox) |
| 13 | Grand Central | 軌道は**pixel座標**(ワールド座標なし、キャリブレーション未提供) |
| 15 | ATC | 体の**facing angle**あり (eye gazeではない)。グループラベルは**このデータセットにはない**(ATR Groupが別途提供) |
| 17 | JRDB | ベースにはsocial/group/poseなし。**JRDB-Act, JRDB-Pose, JRDB-Social** が別途レイヤーとして追加 |
| 19 | WildTrack | **ground-plane world座標**提供 (pixel bboxだけではない) |
| 20 | Oxford Town Centre | **BMVC 2011** (2009ではない)。頭部は**bboxのみ**(向き角アノテーションなし) |
| 23 | Argoverse 2 | **NeurIPS 2021** Datasets track (2023ではない) |
| 25 | RPEE-Heads | 頭部**検出**のみ (向き角/追跡なし) |
| 26 | CcHead | 頭部**追跡**のみ (向き角なし) |
| 28 | AMASS | 体モデルは**SMPL+H** (手の関節を含む、SMPLではない) |
| 27 | TBD Pedestrian | 公開ドキュメントが限定的。SocNavBenchシナリオの可能性あり |

---

## ギャップ分析

### 全部揃っているデータセットは存在しない

| 理想の組み合わせ | 最も近い | 欠けているもの |
|-----------------|---------|---------------|
| Eye Gaze + 群衆 + グループ + 全身 | THÖR-MAGNI | 全身骨格なし、室内制御環境、16/40人のみ視線 |
| Eye Gaze + 全身 + 3Dシーン | Nymeria / GIMO | 群衆なし (単体行動のみ) |
| 全身骨格 + 群衆 | Waymo-3DSkelMo | 視線なし、骨格は推定値、自動運転文脈 |
| グループ + 社会行動 + 3D | JRDB ecosystem | eye gaze なし、全身骨格なし (2Dキーポイントのみ) |
| **全部** | **存在しない** | — |

---

## Sources

- [Nymeria](https://www.projectaria.com/datasets/nymeria/) / [arXiv](https://arxiv.org/abs/2406.09905)
- [GIMO](https://geometry.stanford.edu/projects/gimo/) / [GitHub](https://github.com/y-zheng18/GIMO)
- [GaMo](https://ieeexplore.ieee.org/document/10972869/)
- [MoGaze](https://humans-to-robots-motion.github.io/mogaze/) / [arXiv](https://arxiv.org/abs/2011.11552)
- [THÖR](http://thor.oru.se/thor.html) / [arXiv](https://arxiv.org/abs/1909.04403)
- [THÖR-MAGNI](http://thor.oru.se/magni.html) / [IJRR](https://journals.sagepub.com/doi/full/10.1177/02783649241274794)
- [MuseumVisitors](https://www.micc.unifi.it/resources/datasets/museumvisitors/)
- [EgoCampus](https://arxiv.org/abs/2512.07668)
- [ETH/UCY](https://icu.ee.ethz.ch/research/datsets.html)
- [SDD](https://cvgl.stanford.edu/projects/uav_data/)
- [TrajNet++](https://github.com/vita-epfl/trajnetplusplusdataset)
- [Grand Central](https://www.ee.cuhk.edu.hk/~xgwang/grandcentral.html)
- [Lyon Festival](https://www.nature.com/articles/s41597-025-04732-3)
- [ATC](https://dil.atr.jp/crest2010_HRI/ATC_dataset/)
- [ATR Group](https://dil.atr.jp/ISL/sets/groups/)
- [JRDB](https://jrdb.erc.monash.edu/)
- [STCrowd](https://arxiv.org/abs/2204.01026)
- [WildTrack](https://www.epfl.ch/labs/cvlab/data/data-wildtrack/)
- [Oxford Town Centre](https://academictorrents.com/details/35e83806d9362a57be736f370c821960eb2f2a01)
- [Waymo-3DSkelMo](https://arxiv.org/abs/2508.09404)
- [nuScenes](https://www.nuscenes.org/)
- [Argoverse 2](https://www.argoverse.org/)
- [Waymo Open](https://waymo.com/open/)
- [RPEE-Heads](https://arxiv.org/abs/2411.18164)
- [CcHead](https://arxiv.org/abs/2408.05877)
- [TBD Pedestrian](https://tbd.ri.cmu.edu/resources/tbd-social-navigation-datasets/)
- [AMASS](https://amass.is.tue.mpg.de/)
- [BABEL](https://babel.is.tue.mpg.de/)
- [Motion-X](https://motion-x-dataset.github.io/)
- [Motion-X++](https://arxiv.org/abs/2501.05098)
- [OpenTraj](https://github.com/crowdbotp/OpenTraj)
- [trajdata](https://github.com/NVlabs/trajdata)
