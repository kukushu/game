# 统一实验分支与历史版本

2026-10-28 起，Python 验证项目只维护 `experiment/city-economy-lab`。原来的六个实验分支是一条连续提交链；最新版本已包含全部阶段，因此无需合并或重写历史。旧分支名清理后，各阶段仍能通过以下标签访问。

| 原分支 | 历史标签 | 提交 |
| --- | --- | --- |
| `experiment/python-city-economy` | `cityeconomylab/baseline` | `628c173` |
| `experiment/retail-intermediation` | `cityeconomylab/retail` | `ea61e0a` |
| `experiment/playable-web` | `cityeconomylab/web-prototype` | `b8985ac` |
| `experiment/land-and-location` | `cityeconomylab/land-location` | `2a67005` |
| `experiment/landlab-player-interface` | `cityeconomylab/player-interface` | `621d0a3` |
| `experiment/blank-city-start` | `cityeconomylab/blank-start` | `80721da` |

所有模块、启动器、研究结论和已提交的对照数据均保留。正式游戏 `main` 不变，实验没有合并进正式游戏逻辑。

## 换电脑使用

```sh
git clone --branch experiment/city-economy-lab --single-branch https://github.com/kukushu/game.git CityEconomyLab
cd CityEconomyLab
```

双击 `run-land.cmd` 进入默认空白城市；其他实验仍使用原启动器，详见 README。

已有独立实验副本可先保存本机未提交修改，再切换到统一分支：

```sh
git fetch origin
git switch --track origin/experiment/city-economy-lab
```

查看历史阶段（只读）：

```sh
git fetch origin --tags
git show cityeconomylab/baseline:README.md
git log --oneline experiment/city-economy-lab
```

本机未提交存档仍留在本机；本次整理没有删除或改写存档。
