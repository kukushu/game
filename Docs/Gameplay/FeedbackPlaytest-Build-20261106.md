# 首轮反馈试玩交付记录

## 2026-11-06 玩家首测修订

当前交付替换为 `Temp/FeedbackPlaytest-20261106-002630/Player/HarborCity.exe`，源码 `cbb726499d2a4ed0d6f94d31d2d6f6481a08fcd4`，窗口版本显示 `cbb72649`。下方 `000254` 为首包历史记录。

玩家截图显示“目标50× / 当前实际50×”提示持续跟随鼠标。原实现直接读取全局 GUI.tooltip，没有用当前鼠标所在控件约束绘制；现改为每次绘制清空本地提示，仅显式命中控件边界时赋值，并按文字内容计算框高。地图区域没有提示来源时不绘制提示框。实际是否消失仍待玩家手测，不把代码归因称为运行复现。

按玩家要求，左上角增加“人口 X · Y 户”，户数为有住址且至少一名存活成员的实际入住家庭；利用已有家庭/成员通知增量维护，读档/切城重建，不逐帧扫描全城居民。首行调整人口/户数、车辆、资金的宽度分配。

本次仍只做代码检查、模型库编译、90组反馈文案键/参数静态核对、Release编译构建及206个产物文件/构建输入哈希核对。构建日志 `Temp/FeedbackPlaytest-20261106-002630/unity-build.log` 为 Success、退出0，无C#编译错误。包大小134,840,484字节，输入指纹 `8405767a2942fdd207b0c5096f43e2526f0c9648ad062f7910e6086777f210be`；产品名与案例仍隔离。未启动 Player，未运行回归、模拟、性能或自动交互；提示消退、户数变化、布局均待玩家手测。

## 首包历史记录

完成范围：常驻城市提醒、按类别/严重程度分组并定位的问题清单、家庭采购原因/企业累计账目、独立产品名、两个可选既有诊断现场、暂停并记录现场。未改变经济、购物、投资参数；未修复既有经营失衡。玩法入口与手动清单见[试玩说明](FeedbackPlaytest-20261105.md)，派生统计及释放边界见[架构说明](../Architecture/PlayerFeedback-20261105.md)。

## 精确版本与产物

- 分支：`codex/gameplay-two-goods-20261104`；起点 `d946159`。实现 `d0455db`，玩家表达/城市上下文 `52b96ce`，保存失败明确提示/旧路径清理 `12aa2f3`。未合并 develop。
- 最终 Player 源码：`12aa2f316dd49641afafaa8e6a262a4a412780d1`，构建时业务工作区无修改；原无关 `Docs/Performance/Current100k-20261103/` 保留且不计入本次提交。
- 构建时间：2026-11-06 00:02:55（北京时间），Unity 6000.6.3f1，Windows64 Mono Release，1440×900 窗口，BuildOptions.None，无诊断归因/轮询参考附加宏。
- 最终目录：`Temp/FeedbackPlaytest-20261106-000254/Player`，入口 `HarborCity.exe`。整个目录206个文件，共134,839,236字节；不要只复制 exe。
- 资源/源码指纹：`2ccb726b56420031d5883a5ae5eaceb6a5e42c3ded7addb09dd2e861af81f735`；精确文件清单在同级 `build-manifest.json`、`artifact-files.json`。
- 实际业务程序集 `HarborCity_Data/Managed/Assembly-CSharp.dll` SHA256：`5f3df3e0d8264f52e28c7578836eaa321279bd95e1be678d464e6fa0d46cccef`。
- 重建命令：`./Tools/BuildFeedbackPlaytest.ps1`，输出新目录；依赖本地保留的原诊断存档。临时工程为本工作树下 `Temp/FeedbackUnity`，没有切换或修改日常 `test` 工程。

## 实际检查与边界

|项目|本轮结果|
|---|---|
|模型库编译|Mono csc library 编译成功，没有执行库或模型；保留原 ReferencePolling 未赋值编译警告|
|Unity 编译/Release 构建|最终日志 `Temp/FeedbackPlaytest-20261106-000254/unity-build.log`：Build Finished, Result: Success，batchmode退出0，无 C# 编译错误|
|静态代码/本地化|git diff --check；PowerShell解析；89个新增中英文 Key 与占位参数一致；无重复 Key|
|产物|206个文件大小/哈希逐项核对；全部构建输入仍符合指纹；exe为x64 PE；实际 app.info 为 DefaultCompany / HarborCity Feedback Playtest|
|两个案例|原文件只读复制；包内 SHA256 与源一致，头部城市格式19；未推进模拟生成场景|
|日常工程与存档|test 仍是 develop/0342fde，原工作区修改原样保留；未运行 Player，不访问日常默认存档|
|模型/游戏回归、模拟、性能、自动交互|按用户要求全部未运行|
|实际手动验收|尚未进行：提醒消退、定位/输入阻挡、窄屏布局、读档/切城、截图、合法保存失败、记录后暂停等均待玩家试玩|

案例来源为 `Temp/TwoGoodsDiagnosis-20261105/wrapper-verified/zero-14.hcity` 和 `final.hcity`，并非新生成或修复后的场景。分别封装为“商店资金耗尽”和“城市供应停摆”，不会自动加载。进入前有保存当前城或明确放弃未保存进度的入口。

先前两次构建目录保留为中间产物；本记录只指定 `000254` 为最终交付。构建成功和静态核对不能替代用户七项试玩验收，也不证明经济平衡或性能收益。故障记录会明确区分“存档未生成”和其他部分失败，实际错误现场路径仍待手测。
