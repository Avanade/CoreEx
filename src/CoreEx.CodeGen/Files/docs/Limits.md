# Shared file-processing limits



## Properties
The `Limits` object supports a number of properties that control the generated code output. The following properties with a bold name are those that are more typically used (considered more important).

Property | Description
-|-
`maxInputBytes` | Positive input-byte budget.<br/>&dagger; Defaults to 67108864 (64 MiB).
`maxUnits` | Positive business-unit budget.<br/>&dagger; Defaults to 100000.
`maxDiagnostics` | Positive diagnostic budget.<br/>&dagger; Defaults to 1000; one terminal limit diagnostic may follow.
