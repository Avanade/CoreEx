# File record property



## Property categories
The `Property` object supports a number of properties that control the generated code output. These properties are separated into a series of logical categories.

Category | Description
-|-
[`Primary`](#Primary) | Wire property identity.
[`Mapping`](#Mapping) | CSV selector and conversion.
[`Validation`](#Validation) | Pure typed validation.

The properties with a bold name are those that are more typically used (considered more important).

## Primary
Wire property identity.

Property | Description
-|-
**`name`** | C# property name. [Mandatory]
`type` | Logical type. Valid options are: `String`, `Integer`, `Int64`, `Decimal`, `Double`, `Boolean`, `Guid`, `DateOnly`, `DateTime`.<br/>&dagger; Defaults to String. All wire properties are nullable; requiredness is separate.

## Mapping
CSV selector and conversion.

Property | Description
-|-
`index` | Zero-based CSV index.<br/>&dagger; Required for routed files. Specify exactly one of index or column.
`column` | CSV column name.<br/>&dagger; Flat files only; specify exactly one of column or index.
`format` | Exact invariant date format.<br/>&dagger; Required for DateOnly/DateTime; not applicable to other types.

## Validation
Pure typed validation.

Property | Description
-|-
`required` | Reject null.<br/>&dagger; Defaults to false. Uses NotNull, not Mandatory, for value types.
`maxLength` | Maximum string length.<br/>&dagger; Nonnegative; String properties only. Zero permits only empty strings or null.
`mandatory` | Reject null and default values.<br/>&dagger; Defaults to false. Unlike required, rejects zero and false.
`minLength` | Minimum string length.<br/>&dagger; Nonnegative; String only.
`regex` | Regular expression.<br/>&dagger; String only; generated cached regex has a one-second timeout.
`email` | Email validation.<br/>&dagger; String only.
`min` | Inclusive minimum. JSON types: `string`, `number`.<br/>&dagger; String, numeric or date value; dates use the property format. String ordering is culture-sensitive.
`max` | Inclusive maximum. JSON types: `string`, `number`.<br/>&dagger; String, numeric or date value; dates use the property format.
`precision` | Decimal precision.<br/>&dagger; Decimal only; from 1 through 29. No rounding.
`scale` | Decimal scale.<br/>&dagger; Decimal only; from 0 through 28, not greater than precision.
`options` | Allowed values. Item JSON types: `string`, `number`, `boolean`.<br/>&dagger; Nonempty typed scalars; dates use the property format. Values are not canonicalized.
`referenceData` | Reference-data code validation. The corresponding [`ReferenceData`](ReferenceData.md) object.<br/>&dagger; Uses the existing CoreEx ReferenceData rule without converting the code to an object. The caller must initialize the ambient reference-data orchestrator and load this collection before processing.
