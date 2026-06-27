# Spec — add-pc-mask-and-shipin-parsers

## ADDED Requirements

### Requirement: PC Mask Item Parser

The system SHALL parse PC `mask.txt` rows into `ItemDefinition` objects using the established 46-column PC equipment item format.

#### Scenario: Mask parser reads item identity and detail type

- **GIVEN** a valid PC mask row with `ItemGenre=0`, `DetailType=11`, and `ParticularType=0`
- **WHEN** the mask parser parses the row
- **THEN** the resulting item keeps `itemGenre=0`, `detailType=11`, and `particularType=0`
- **AND** the item has non-empty raw/normalized Vietnamese name fields
- **AND** icon source id is derived from the PC SPR path column.

### Requirement: PC Shipin Item Parser

The system SHALL parse PC `shipin.txt` rows into `ItemDefinition` objects using the established 46-column PC equipment item format.

#### Scenario: Shipin parser reads item identity and detail type

- **GIVEN** a valid PC shipin row with `ItemGenre=0`, `DetailType=14`, and `ParticularType=0`
- **WHEN** the shipin parser parses the row
- **THEN** the resulting item keeps `itemGenre=0` and `detailType=14`
- **AND** it has non-empty raw/normalized Vietnamese name fields
- **AND** icon source id is derived from the PC SPR path column.

### Requirement: Batch Loader Includes Mask and Shipin

`PcItemBatchLoader.LoadAll` SHALL include `mask` and `shipin` in the per-file load result and imported item bundle.

#### Scenario: Reference batch loads sixteen item files

- **GIVEN** the reference PC item sample directory
- **WHEN** `PcItemBatchLoader.LoadAll` is called
- **THEN** `perFileCounts` contains 16 keys including `mask` and `shipin`
- **AND** both new keys have at least five parsed rows.

### Requirement: Mask ParticularType Zero Is Preserved

`PcItemBatchLoader.ApplyCategoryIds` SHALL NOT rewrite `particularType=0` for mask rows because PC mask uses zero as a valid unique particular type.

#### Scenario: Mask row zero remains zero

- **GIVEN** a mask item with `detailType=11` and `particularType=0`
- **WHEN** category IDs are applied for stem `mask`
- **THEN** the item keeps `particularType=0`.

### Requirement: Shipin Rows Remain Importable Despite Repeated Zero

`PcItemBatchLoader.ApplyCategoryIds` SHALL keep the existing zero-to-row-index fallback for shipin rows because sample PC rows repeat `ParticularType=0` and importer keys must remain unique.

#### Scenario: Shipin zero receives row index fallback

- **GIVEN** two shipin items with `detailType=14` and `particularType=0`
- **WHEN** category IDs are applied for stem `shipin`
- **THEN** their resulting `particularType` values differ and can be imported without tuple collision.

### Requirement: Equipment Test Categorization

New tests for this change SHALL be category-filterable via NUnit category `Equipment` and MUST NOT require the full EditMode suite in the inner dev loop.

#### Scenario: Equipment category covers parser integration

- **GIVEN** the parser integration tests
- **WHEN** Unity EditMode tests run with `category_names=["Equipment"]`
- **THEN** the new mask/shipin parser and batch loader behavior is included.
