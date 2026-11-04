# Hierarchical file structure



## Properties
The `Structure` object supports a number of properties that control the generated code output. The following properties with a bold name are those that are more typically used (considered more important).

Property | Description
-|-
**`root`** | Business root. The corresponding [`Node`](Node.md) object. [Mandatory]
`header` | First envelope. The corresponding [`Node`](Node.md) object.<br/>&dagger; One or Optional, without children.
`trailer` | Final envelope. The corresponding [`Node`](Node.md) object.<br/>&dagger; One or Optional, without children.
