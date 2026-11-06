# Reference-data code validation



## Properties
The `ReferenceData` object supports a number of properties that control the generated code output. The following properties with a bold name are those that are more typically used (considered more important).

Property | Description
-|-
**`type`** | Reference-data CLR type. [Mandatory]<br/>&dagger; An existing IReferenceData type, for example Contoso.Contracts.ReferenceData.Country. The wire property remains string?.
`allowInactive` | Allow inactive codes.<br/>&dagger; Defaults to false. Unknown codes always fail validation.
