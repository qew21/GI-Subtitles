# 原神双语字幕插件

基于[PaddleOCRSharp](https://github.com/raoyutian/PaddleOCRSharp)文本识别和[Genshin_Datasets](https://github.com/AI-Hobbyist/Genshin_Datasets)原神多语言文本json内容。

## 介绍

期望在展示单一语言剧情文本时，可以同事展示其他语言的对应文本，如中->英， 英->中， 日->中等。

有时候可能喜欢某一语言配音，但对文本理解可能出现偏差。

由于PaddleOCR的限制，本项目只能在64位带avx指令集上的CPU上使用。

## 原理

首先用OCR识别剧情文本，  
然后采用Levenshtein距离匹配现有语言包中文本中对应的名称，  
再根据名称找到其他语言包中的文本展示出来。


## 示例

https://www.bilibili.com/video/BV1sH4y1e7Qg

