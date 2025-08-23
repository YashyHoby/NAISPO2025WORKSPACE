ナイス。Windows 11＋Unity 6.0（Universal 2D）向けに、**そのまま貼るだけ**の設定を用意しました。以下の順に進めればOKです。

---

# 1) Unity 側の一度きり設定

Unity メニュー → **Edit > Project Settings > Editor**

* **Asset Serialization**: `Force Text`
* **Version Control**: `Visible Meta Files`
* （推奨）**Line Endings**: `Unix (LF)`

> これで `.unity / .prefab / .anim` などが YAML（テキスト）になり、差分・マージがしやすくなります。`.meta` も必ず追跡されます。

---

# 2) .gitignore（プロジェクト直下に作成）

```gitignore
# --- Unity 再生成系（絶対コミットしない） ---
[Ll]ibrary/
[Tt]emp/
[Oo]bj/
[Bb]uild*/
[Bb]uilds*/
[Ll]ogs/
[Uu]ser[Ss]ettings/
MemoryCaptures/
SysInfo.txt

# --- IDE / 自動生成 ---
.vs/
.idea/
.vscode/*
!.vscode/settings.json
!.vscode/extensions.json
*.csproj
*.sln
*.user
*.pidb
*.mdb
*.opendb
*.VC.db

# --- Rider/VS キャッシュ ---
*.DotSettings.user
.gradle/

# --- キャッシュ類 ---
# Unity 6 以降のアーティファクトや各種キャッシュが増えても基本ここで吸収
# 必要になったら都度追加

# --- コミット対象（除外しない） ---
# Assets/
# Packages/
# ProjectSettings/
# (任意) .gitignore / .gitattributes / README.md

# --- Addressables を使う場合の一例（binは生成物なので通常除外推奨） ---
# Assets/AddressableAssetsData/*/*.bin*
```

---

# 3) Git LFS（大きいバイナリを追跡）

> 2Dテンプレートでは **アート原本（.psd/.psb）や完成画像（.png）・音** が重くなりがち。YAML系（.unity/.prefab/.anim など）は**LFSに入れない**でOK。

まず一度だけ:

```bash
git lfs install
```

追跡ルール（Universal 2D想定・必要に応じて増減可）:

```bash
git lfs track "*.png" "*.jpg" "*.jpeg" "*.tga" "*.tif" "*.tiff" "*.exr" "*.svg"
git lfs track "*.psd" "*.psb"    # アート原本はLFS必須
git lfs track "*.wav" "*.mp3" "*.ogg" "*.aiff" "*.aif"
git lfs track "*.mp4" "*.mov"
git lfs track "*.ttf" "*.otf"    # フォントも大きいことがある
```

> **ロック運用したい場合**（共同編集を防ぐ）：`.psd/.psb` は「ロック可能」にしておくと安全です（下の `.gitattributes` に反映済み）。

---

# 4) .gitattributes（改行・マージ・LFS・ロック）

```gitattributes
# ---- 改行統一（WindowsでもLFへ） ----
*           text=auto eol=lf
*.bat       text eol=crlf
*.ps1       text eol=crlf
*.cs        text eol=lf
*.shader    text eol=lf
*.cginc     text eol=lf
*.compute   text eol=lf
*.json      text eol=lf
*.yaml      text eol=lf
*.yml       text eol=lf

# ---- Unity YAML系はSmartMerge（UnityYAMLMerge）を使う ----
*.unity       merge=unityyamlmerge eol=lf
*.prefab      merge=unityyamlmerge eol=lf
*.mat         merge=unityyamlmerge eol=lf
*.anim        merge=unityyamlmerge eol=lf
*.controller  merge=unityyamlmerge eol=lf
*.asset       merge=unityyamlmerge eol=lf
*.spriteatlas* merge=unityyamlmerge eol=lf   # 2Dで生成されることがある

# ---- .meta は union で安全併合 ----
*.meta        merge=union eol=lf

# ---- LFS（追跡コマンドで設定済みでも明記推奨） ----
*.png  filter=lfs diff=lfs merge=lfs -text
*.jpg  filter=lfs diff=lfs merge=lfs -text
*.jpeg filter=lfs diff=lfs merge=lfs -text
*.tga  filter=lfs diff=lfs merge=lfs -text
*.tif  filter=lfs diff=lfs merge=lfs -text
*.tiff filter=lfs diff=lfs merge=lfs -text
*.exr  filter=lfs diff=lfs merge=lfs -text
*.svg  filter=lfs diff=lfs merge=lfs -text
*.psd  filter=lfs diff=lfs merge=lfs -text lockable
*.psb  filter=lfs diff=lfs merge=lfs -text lockable
*.wav  filter=lfs diff=lfs merge=lfs -text
*.mp3  filter=lfs diff=lfs merge=lfs -text
*.ogg  filter=lfs diff=lfs merge=lfs -text
*.aiff filter=lfs diff=lfs merge=lfs -text
*.aif  filter=lfs diff=lfs merge=lfs -text
*.mp4  filter=lfs diff=lfs merge=lfs -text
*.mov  filter=lfs diff=lfs merge=lfs -text
*.ttf  filter=lfs diff=lfs merge=lfs -text
*.otf  filter=lfs diff=lfs merge=lfs -text
```

---

# 5) Unity SmartMerge を Windows で有効化

**方法A：パスがわかる場合（例：6000.0.0f1）**

```bash
git config --global merge.unityyamlmerge.name "Unity SmartMerge"
git config --global merge.unityyamlmerge.driver "\"C:/Program Files/Unity/Hub/Editor/6000.0.0f1/Editor/Data/Tools/UnityYAMLMerge.exe\" merge -p %O %A %B %A"
```

**方法B：PowerShellで自動検出（最新版を拾う）**

```powershell
$yaml = Get-ChildItem "C:\Program Files\Unity\Hub\Editor" -Directory |
  Sort-Object Name -Descending |
  ForEach-Object { Join-Path $_.FullName "Editor\Data\Tools\UnityYAMLMerge.exe" } |
  Where-Object { Test-Path $_ } | Select-Object -First 1

git config --global merge.unityyamlmerge.name "Unity SmartMerge"
git config --global merge.unityyamlmerge.driver "`"$yaml`" merge -p %O %A %B %A"
```

> これで `.gitattributes` に指定した拡張子（.unity/.prefab 等）の競合時、**先に UnityYAMLMerge が自動マージ**してくれます。

---

# 6) 初期化〜初回プッシュ（PowerShell or Git Bash）

```bash
git init
git branch -M main
git add .gitattributes .gitignore
git add Assets Packages ProjectSettings
git commit -m "Initial commit (Unity 6.0 Universal 2D)"
git remote add origin https://github.com/<ユーザー名>/<リポジトリ名>.git
git push -u origin main
```

---

# 7) クローンした人の初回手順（READMEに書いておくと親切）

1. リポジトリをクローン（`--recursive` は不要でOK）
2. **LFS を有効化**：`git lfs install` → `git lfs fetch`（必要なら）
3. Unity Hub で該当エディタ（6.0.x）を入れ、プロジェクトを開く
   → `Library/` は自動再生成（時間がかかっても放置でOK）
4. もしマージ時に YAMLMerge が動かない場合は **手元でも 5) を実行**

---

# 8) Universal 2D（URP 2D）ならではのメモ

* **URP アセット / 2D Renderer Data**（`Assets/<任意>/Settings/*.asset` など）は YAML なので**必ずコミット対象**。
* `Packages/manifest.json` と `packages-lock.json` は**必ずコミット**（環境再現用）。
* スプライトは基本 `.png` を LFS、原本は `.psd/.psb` を LFS＋**lock運用**が安全：

  ```bash
  # 編集前にロック
  git lfs lock Assets/Sprites/hero.psd
  # 解除
  git lfs unlock Assets/Sprites/hero.psd
  ```

---

# 9) ありがちトラブル対策（超要約）

* **Library を入れてしまった**

  ```bash
  git rm -r --cached Library
  git commit -m "Remove Library from repo"
  git push
  ```
* **改行差分が暴れる** → `.gitattributes` で LF 固定＋エディタ設定を LF に。
* **.meta 欠落で参照崩壊** → `Visible Meta Files` にして `.meta` も必ずコミット。
* **LFS 容量オーバー** → 不要バイナリ削除・履歴整理・課金 or 外部ストレージ検討。

---

必要なら、この内容を\*\*そのまま置ける一式（.gitignore / .gitattributes / README.md）\*\*をプロジェクト名入りで作って渡します。リポジトリURL（予定）と、使っているIDE（VS / Rider / VSCode）だけ教えてください。
