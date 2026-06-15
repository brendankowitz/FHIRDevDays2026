# Ignixa.FhirFakes

## Overview

**Ignixa.FhirFakes** is a comprehensive synthetic FHIR data generation library for .NET, designed for testing, development, and demonstration in healthcare IT. It generates realistic, clinically meaningful FHIR resources with proper demographics, terminology codes, and patient journey modeling — all within a .NET-native ecosystem.

Unlike template-based generators that produce static or repetitive test data, FhirFakes uses a **4-layer generation architecture** that balances control and realism. At the lowest layer, it generates random but structurally valid FHIR resources using schema metadata. At the highest layer, it produces large-scale patient populations with realistic demographic distributions and simulated clinical histories.

The library is part of the broader **Ignixa FHIR ecosystem** maintained by Brendan Kowitz. It can be used as a standalone NuGet package, via its CLI tool (`ignixa-fakes`), or integrated directly into .NET test suites and CI pipelines.

| Attribute | Value |
|-----------|-------|
| **Package ID** | `Ignixa.FhirFakes` |
| **Latest Version** | `0.0.163` |
| **Target Framework** | .NET 9.0 |
| **License** | MIT |
| **Repository** | `https://github.com/brendankowitz/ignixa-fhir` |
| **Documentation** | `https://brendankowitz.github.io/ignixa-fhir/docs/core-sdk/fhir-fakes` |
| **NuGet Profile** | `https://www.nuget.org/packages/Ignixa.FhirFakes` |

---

## Architecture & 4-Layer Generation Model

FhirFakes is organized around a stacked architecture where each layer builds upon the one below it, offering progressively higher levels of abstraction and clinical realism.

```
+--------------------------------------------------+
|  Layer 4: Population Generators                  |
|  (PopulationGenerator - large scale)             |
+--------------------------------------------------+
|  Layer 3: Scenarios & Predefined                 |
|  (ScenarioBuilder, clinical journeys)            |
+--------------------------------------------------+
|  Layer 2: States & Builders                      |
|  (PatientBuilder, ObservationBuilder)            |
+--------------------------------------------------+
|  Layer 1: Schema-Based Generation                |
|  (SchemaBasedFhirResourceFaker)                  |
+--------------------------------------------------+
```

| Layer | Namespace | Purpose | Control Level |
|-------|-----------|---------|---------------|
| **1** | `Ignixa.FhirFakes` | Random but valid FHIR resources | Low — schema-driven |
| **2** | `Ignixa.FhirFakes.Builders` | Fluent builders for specific resource types | Medium — resource-specific |
| **3** | `Ignixa.FhirFakes.Scenarios` | Complete clinical scenarios with journeys | High — clinical coherence |
| **4** | `Ignixa.FhirFakes.Population` | Large-scale realistic populations | Very High — demographically accurate |

This design lets developers choose the right abstraction for their use case:
- **Unit tests** might use Layer 1 or 2 for quick resource creation
- **Integration tests** might use Layer 3 for clinically coherent bundles
- **Load testing / analytics** might use Layer 4 for realistic population datasets

---

## Layer 1: Schema-Based Resource Generation

### SchemaBasedFhirResourceFaker

**File**: `src/Core/Ignixa.FhirFakes/SchemaBasedFhirResourceFaker.cs`

The foundational layer uses `IFhirSchemaProvider` metadata to intelligently generate FHIR resources. It does NOT use hardcoded JSON templates. Instead, it:

1. Retrieves the `IType` definition from the schema provider for the requested resource type
2. Iterates through children (elements) to find required vs optional elements
3. Uses **binding information** from `ITypeExtended` to select appropriate codes via `BindingCodeMapper`
4. Falls back to element name + datatype heuristics when bindings are unavailable
5. Generates JSON respecting cardinality (`IsCollection`, `IsRequired`)

**Key Design Decision**: Optional elements are **never** populated by default. This provides deterministic behavior for test stability. If tests need optional fields, they must use Layer 2 builders or manually set them after generation.

```csharp
public class SchemaBasedFhirResourceFaker
{
    private const int MaxRecursionDepth = 5;  // Prevents infinite recursion in complex types
    
    public ResourceJsonNode Generate(string resourceType)
    public ResourceJsonNode CreatePatient(Action<PatientBuilder>? configure = null)
    public ResourceJsonNode CreateSeattlePatient(Action<PatientBuilder>? configure = null)
    public SchemaBasedFhirResourceFaker WithTag(string? tag)  // Test isolation
}
```

### BindingCodeMapper

**File**: `src/Core/Ignixa.FhirFakes/BindingCodeMapper.cs`

A critical internal component that maps FHIR value set bindings to predefined clinical terminology codes. This ensures generated resources use **terminology-correct** codes rather than random strings:

| Value Set URI | Mapped Code Source |
|---------------|-------------------|
| `http://hl7.org/fhir/ValueSet/observation-codes` | `LabObservations` + `VitalSigns` (LOINC) |
| `http://hl7.org/fhir/ValueSet/procedure-code` | `Procedures` (SNOMED CT) |
| `http://hl7.org/fhir/ValueSet/allergyintolerance-code` | `Allergens` (SNOMED CT) |
| `http://hl7.org/fhir/ValueSet/vaccine-code` | `Immunizations` (CVX) |
| `http://hl7.org/fhir/ValueSet/medication-codes` | `FhirCode.Medications` (RxNorm) |
| `http://hl7.org/fhir/ValueSet/condition-code` | `FhirCode.Conditions` (SNOMED CT) |
| `http://hl7.org/fhir/ValueSet/report-codes` | `DiagnosticReports` (LOINC) |
| `http://hl7.org/fhir/ValueSet/encounter-type` | `FhirCode.EncounterTypes` |
| `http://hl7.org/fhir/ValueSet/observation-vitalsignresult` | `VitalSigns` (LOINC) |

The mapper uses **reflection** to lazily cache code arrays from static properties on the code classes. It also falls back to `IValueSetProvider` for administrative/structural value sets (e.g., `administrative-gender`).

---

## Layer 2: Builders & Fluent API

### FhirResourceBuilder<T> (CRTP Base Class)

**File**: `src/Core/Ignixa.FhirFakes/Builders/FhirResourceBuilder.cs`

Uses the **Curiously Recurring Template Pattern (CRTP)** to enable fluent APIs in derived builders:

```csharp
public abstract class FhirResourceBuilder<TBuilder>
    where TBuilder : FhirResourceBuilder<TBuilder>
{
    protected JsonObject BuildMeta()           // versionId, lastUpdated, tag, profile
    protected static JsonObject CreateReference(string resourceType, string id)
    protected static JsonObject CreateCodeableConcept(string code, string system, ...)
    protected static JsonObject CreateIdentifier(string value, string system, string? use)
    protected static JsonObject CreateContactPoint(string system, string value, string? use)
    protected static JsonObject CreateAddress(string line, string city, string state, string postalCode, string country)
    
    public TBuilder WithId(string id)
    public TBuilder WithTag(string tag)          // http://ignixa.dev/test-isolation
    public TBuilder WithProfile(string profileUrl)
}
```

### PatientBuilder

**File**: `src/Core/Ignixa.FhirFakes/Builders/PatientBuilder.cs`

The most sophisticated builder, supporting **two modes**:

**Mode 1: Simple** — Basic Bogus-based randomization for simple tests
**Mode 2: Realistic** — Real US demographics with ethnically appropriate names for population generation

**Fluent API Methods:**

```csharp
// Demographics
.WithAge(int age)
.WithBirthYear(int year)
.WithBirthDate(int year)                    // FHIR year-only: "1982"
.WithBirthDate(int year, int month)         // FHIR month precision: "1982-01"
.WithBirthDate(int year, int month, int day) // FHIR full date: "1982-01-15"
.WithGender(Func<PatientBuilderSelectors.Gender, string> selector)  // g => g.Male
.WithGender(string gender)                  // "male", "female", "other", "unknown"
.WithGivenName(string name)
.WithFamilyName(string name)

// Geographic (auto-populated from city demographics)
.FromCity(CityDemographics city)
.FromCity(string city, string state)
.FromSeattle()
.WithCity(string city)
.WithState(string state)
.WithCountry(string country)
.WithZipCode(string zip)
.WithAreaCode(string areaCode)
.WithStreetAddress(string address)

// Clinical
.WithRealisticBMI()                         // Generates BMI based on age/gender distribution
.WithBMI(decimal bmi)
.WithActive(bool active)

// Multiple birth
.WithMultipleBirth(int order)               // Integer: birth order
.WithMultipleBirth(bool isMultiple)          // Boolean: is multiple birth

// References
.WithManagingOrganization(string orgId)
.WithGeneralPractitioner(string practitionerId)

// Identifiers
.WithTypedIdentifier(string value, string system, string code, string display)

// Profile system
.WithProfile(IPatientProfile profile)
.WithAttribute(string key, object value)

// Build
.Build()                                    // Returns ResourceJsonNode
```

### Other Builders

| Builder | Key Methods |
|---------|-------------|
| `ObservationBuilder` | Vital signs, lab observations, component-based observations, reference ranges |
| `OrganizationBuilder` | `WithName`, `WithNpi`, `WithTaxId`, `WithAddress`, `WithType` |
| `PractitionerBuilder` | `WithName`, `WithNpi`, `WithSpecialty`, `WithIdentifier` |
| `MedicationRequestBuilder` | `WithMedication`, `WithPatient`, `WithDosageInstruction` |
| `DiagnosticReportBuilder` | `WithCode`, `WithSubject`, `WithResult`, `WithCategory` |
| `CareTeamBuilder` | `WithStatus`, `WithSubject`, `WithParticipant` |
| `LocationBuilder` | `WithName`, `WithAddress`, `WithStatus`, `WithType` |
| `GroupBuilder` | `WithType`, `WithMember`, `WithName` |

---

## Layer 3: Scenarios & Clinical Journeys

### ScenarioBuilder

**File**: `src/Core/Ignixa.FhirFakes/Scenarios/ScenarioBuilder.cs`

**Design Principle: One Scenario = One Patient**. Optimized for creating a single patient with related resources. Uses a **state machine** pattern where each method appends a `ScenarioState` to an internal list, then `Build()` executes all states in sequence.

```csharp
var scenario = new ScenarioBuilder(schemaProvider)
    .WithName("Hypertension Screening")
    .WithDescription("Patient journey for hypertension diagnosis")
    .WithTag(Guid.NewGuid().ToString())              // Test isolation
    .WithUrnUuidReferences()                          // Transaction bundles
    .WithResolvedReferences()                         // Batch bundles
    
    // Patient configuration
    .WithPatient(p => p
        .WithAge(55)
        .WithGender(g => g.Male)
        .FromCity(KnownCities.Boston))
    .WithPatient(age: 45, gender: "female")           // Simple overload
    
    // Timeline events
    .AddEncounter("Annual checkup")
    .AddObservation(VitalSigns.BloodPressureSystolic, 140m, "mmHg")
    .AddConditionOnset(FhirCode.Conditions.HypertensionEssential, severity: 2)
    .AddMedicationOrder(MedicationOrderState.Metformin500mg())
    
    // Diagnostic panels
    .AddComprehensiveMetabolicPanel()
    .AddLipidPanel()
    .AddCompleteBloodCount()
    
    // Temporal progression
    .DelayMonths(3)
    .DelayYears(1)
    
    // Reusable fragments
    .AddSubScenario(CommonScenarios.RecordVitalSigns())
    .AddSubScenario(CommonScenarios.BasicMetabolicPanel())
    
    .Build();
```

### ScenarioContext

**File**: `src/Core/Ignixa.FhirFakes/Scenarios/ScenarioContext.cs`

Holds all generated resources with typed accessors:

```csharp
public sealed class ScenarioContext
{
    public ResourceJsonNode? Patient { get; set; }
    public IReadOnlyList<ResourceJsonNode> Encounters { get; }
    public IReadOnlyList<ResourceJsonNode> Conditions { get; }
    public IReadOnlyList<ResourceJsonNode> Observations { get; }
    public IReadOnlyList<ResourceJsonNode> Medications { get; }
    public IReadOnlyList<ResourceJsonNode> Procedures { get; }
    public IReadOnlyList<ResourceJsonNode> DiagnosticReports { get; }
    public IReadOnlyList<ResourceJsonNode> Immunizations { get; }
    public IReadOnlyList<ResourceJsonNode> Allergies { get; }
    public IReadOnlyList<ResourceJsonNode> AllResources { get; }  // In generation order
    public IReadOnlyList<ScenarioEvent> Timeline { get; }
    
    public BundleJsonNode ToBundle()         // Transaction bundle with urn:uuid refs
    public BundleJsonNode ToBatchBundle()    // Batch bundle with resolved refs
}
```

### Predefined Scenarios (14 Total)

All are extension methods on `IFhirSchemaProvider` in the `Ignixa.FhirFakes.Scenarios.Predefined` namespace:

| # | Scenario | Extension Method | Key Resources |
|---|----------|-----------------|---------------|
| 1 | Type 2 Diabetes | `GetDiabeticPatient(age=52, gender, severity=2)` | Patient, Condition(DiabetesType2), Observations(A1C, Glucose), MedicationRequest(Metformin), Follow-up encounters |
| 2 | Hypertension | `GetHypertensivePatient(age=58, gender, severity=2)` | Patient, Condition(Hypertension), Observations(BP), MedicationRequest(Lisinopril, Amlodipine) |
| 3 | Pregnancy Journey | `GetPregnantPatient()` | Patient, Condition(Pregnancy), Observations(FetalHR, prenatal labs) |
| 4 | Asthma (Pediatric) | `GetAsthmaticChild()` | Pediatric patient, Condition(Asthma), MedicationRequest(Albuterol, Fluticasone) |
| 5 | Wellness Visit | `GetWellnessVisit(age=45, gender="male", includeLipidPanel=true)` | Patient, 7 vital sign Observations, DiagnosticReport(BMP 8 tests), DiagnosticReport(Lipid Panel 4 tests) |
| 6 | Emergency - Chest Pain | `GetChestPainVisit()` | Patient, Emergency Encounter, Observations(Cardiac enzymes, ECG), Condition(Acute MI) |
| 7 | Emergency - Abdominal Pain | `GetAbdominalPainVisit()` | Patient, Emergency Encounter, Observations(Labs, Imaging), Condition(Appendicitis) |
| 8 | Pediatric Ear Infection | `GetPediatricEarInfection()` | Pediatric patient, Condition(Otitis Media), MedicationRequest(Amoxicillin) |
| 9 | UTI | `GetUrinaryTractInfection()` | Patient, Condition(UTI), MedicationRequest(Antibiotics) |
| 10 | Breast Cancer | `GetBreastCancerPathway()` | Patient, Condition(Breast Cancer), Procedures(Biopsy, Mastectomy), MedicationRequests |
| 11 | Acute MI | `GetAcuteMyocardialInfarction()` | Patient, Condition(Acute MI), Procedures(Cardiac Cath), Medications(Aspirin, Clopidogrel) |
| 12 | COPD | `GetCOPDManagementWithExacerbations()` | Patient, Condition(COPD), Multiple encounters, Medication escalations |
| 13 | CKD Progression | `GetChronicKidneyDiseaseProgression()` | Patient, Condition(CKD), Staged progression with labs |
| 14 | Metabolic Syndrome | `GetMetabolicSyndromeProgression()` | Patient, Conditions(Diabetes, Hypertension, Hyperlipidemia), Medications |

**Example - Diabetic Patient Scenario:**

```csharp
var scenario = schemaProvider.GetDiabeticPatient(
    age: 52,
    gender: "male",
    severity: 2);

// Includes:
// - Patient with specified demographics
// - Condition: Type 2 Diabetes
// - Observations: A1C, blood glucose
// - MedicationRequests: Metformin
// - Multiple follow-up encounters
```

### Reusable Scenario Fragments

**File**: `src/Core/Ignixa.FhirFakes/Scenarios/CommonScenarios.cs`

Composable fragments that can be added via `AddSubScenario()`:

| Fragment | Method | Resources Generated |
|----------|--------|-------------------|
| Record Vital Signs | `CommonScenarios.RecordVitalSigns()` | Height, Weight, BMI, BP (systolic + diastolic), Heart Rate |
| Cardiovascular Vitals | `CommonScenarios.CardiovascularVitals()` | Heart Rate, BP, O2 Saturation |
| Basic Metabolic Panel | `CommonScenarios.BasicMetabolicPanel()` | 8 lab observations + DiagnosticReport |
| Lipid Panel | `CommonScenarios.LipidPanel()` | 4 lab observations + DiagnosticReport |
| Complete Blood Count | `CommonScenarios.CompleteBloodCount()` | 6 lab observations + DiagnosticReport |

---

## Layer 4: Population Generation

### PopulationGenerator

**File**: `src/Core/Ignixa.FhirFakes/Population/PopulationGenerator.cs`

Generates large-scale patient populations with realistic demographic distributions. Orchestrates:
- Demographic sampling from real US cities (`DemographicsDataProvider`)
- Culturally appropriate name generation (`LocalBasedNameGenerator` + Bogus locales)
- Full lifecycle simulation from birth to current age (`PatientLifecycleGenerator`)
- Age/race-stratified disease risk modeling (`DiseaseRiskCalculator`)

```csharp
public class PopulationGenerator(IFhirSchemaProvider schemaProvider)
{
    public IReadOnlyList<CityDemographics> AvailableCities { get; }
    public IReadOnlyList<string> AvailableStates { get; }
    // States: Arizona, California, Illinois, Massachusetts, New York, Pennsylvania, Texas, Washington
    
    public IEnumerable<ScenarioContext> Generate(string state, int populationSize)
}
```

**Generation Algorithm:**
1. Select city (weighted by population) from the requested state
2. Sample demographics using `PatientBuilder.FromCity()` → race, age, gender, zip, area code, name
3. Extract sampled demographics for lifecycle simulation
4. Configure `PatientLifecycleGenerator` with birth year, gender, name, zip, area code
5. Add wellness schedule (pediatric if age < 18, adult if age >= 18)
6. Add immunization schedule
7. Add **probabilistic conditions** based on age/race/BMI:
   - Type 2 Diabetes (age 40+, risk from `DiseaseRiskCalculator`)
   - Essential Hypertension (age 35+)
   - Asthma (ages 1-17)
   - Cancer (age 50+, risk calculated)
8. Simulate lifecycle until current age
9. Yield `ScenarioContext`

### DemographicsDataProvider

**File**: `src/Core/Ignixa.FhirFakes/Population/DemographicsDataProvider.cs`

Contains real **US Census Bureau 2020 Census data** for 11 major US cities:

| City | State | Population | Male Ratio | Age Distribution | Zip Prefix | Area Codes |
|------|-------|------------|------------|------------------|------------|------------|
| New York | New York | 8,336,817 | 0.476 | 0-17: 20.8%, 18-44: 45.3%, 45-64: 23.9%, 65+: 10.0% | 100 | 212, 718, 917, 347, 646 |
| Los Angeles | California | 3,979,576 | 0.496 | Similar distribution | 900 | 213, 310, 323, 424, 818 |
| Chicago | Illinois | 2,746,388 | 0.486 | 0-17: 21.4%, 18-44: 46.8%, 45-64: 22.6%, 65+: 9.2% | 606 | 312, 773, 872 |
| Houston | Texas | 2,304,580 | 0.500 | 0-17: 25.8%, 65+: 6.9% | 770 | 713, 281, 832 |
| Phoenix | Arizona | 1,680,992 | 0.501 | 0-17: 25.0%, 65+: 9.1% | 850 | 602, 480, 623 |
| Philadelphia | Pennsylvania | 1,603,797 | 0.475 | 0-17: 21.6%, 65+: 9.5% | 191 | 215, 267 |
| San Antonio | Texas | 1,547,253 | 0.495 | 0-17: 26.6%, 65+: 7.5% | 782 | 210, 726 |
| San Diego | California | 1,423,851 | 0.502 | 0-17: 20.8%, 65+: 9.7% | 921 | 619, 858 |
| Dallas | Texas | 1,343,573 | 0.501 | 0-17: 25.8%, 65+: 5.7% | 752 | 214, 469, 972 |
| Boston | Massachusetts | 675,647 | 0.480 | 0-17: 17.0%, 65+: 12.0% | 021 | 617, 857 |
| Seattle | Washington | 737,015 | 0.503 | 0-17: 16.0%, 65+: 12.0% | 981 | 206, 425 |

Each city includes ethnicity distribution data (White, Black, Hispanic, Asian proportions) used by `USCorePatientProfile`.

### DiseaseRiskCalculator

**File**: `src/Core/Ignixa.FhirFakes/Lifecycle/DiseaseRiskCalculator.cs`

Implements epidemiological risk models based on CDC prevalence data:

| Condition | Age Range | Risk Factors |
|-----------|-----------|--------------|
| Type 2 Diabetes | 40-90 | Age, BMI, smoking (13.7% prevalence), family history (30%) |
| Hypertension | 35-90 | Age, BMI, diabetes status |
| Asthma | 1-17 | Age, allergies (25% prevalence) |
| Cancer | 50+ | Age, smoking, family history |

Uses `Random.Shared.NextDouble()` for probabilistic determination.

---

## Profile & Name Generation System

### IPatientProfile

**File**: `src/Core/Ignixa.FhirFakes/Builders/Profiles/IPatientProfile.cs`

The profile system defines country/region-specific extensions, identifiers, and name generation:

```csharp
public interface IPatientProfile
{
    INameGenerationStrategy NameGenerationStrategy { get; }
    string ProfileUrl { get; }                          // e.g., US Core URL
    string CountryCode { get; }                         // "US", "AU", "NL"
    IEnumerable<string> RequiredAttributes { get; }      // Keys from demographics
    IEnumerable<JsonObject> BuildExtensions(IReadOnlyDictionary<string, object> attributes, decimal? bmi);
    IEnumerable<JsonObject>? BuildIdentifiers(IReadOnlyDictionary<string, object> attributes);
    bool ValidateAttributes(IReadOnlyDictionary<string, object> attributes);
    Dictionary<string, object> SampleProfileAttributes(CityDemographics city);
}
```

### Profile Implementations

| Profile | Country | Extensions | Key Attributes |
|---------|---------|------------|----------------|
| `DefaultPatientProfile` | (none) | BMI only | None |
| `USCorePatientProfile` | "US" | Race, Ethnicity, BMI | `race`, `ethnicity` |
| `AUBasePatientProfile` | "AU" | Indigenous status | `indigenousStatus` |

**USCorePatientProfile** implements:
- `us-core-race` extension with text value
- `us-core-ethnicity` extension for Hispanic origin
- Custom `patient-bmi` extension
- 14 race categories: White, Black, Hispanic, Asian (with subtypes: Chinese, Indian, Filipino, Vietnamese, Korean, Japanese), NativeAmerican, PacificIslander, Arab, Other

### Name Generation Strategy

| Strategy | Description |
|----------|-------------|
| `DefaultNameGenerationStrategy` | Maps country codes to Bogus locales (25+ countries: NL, DE, FR, GB, MX, BR, CN, JP, SA, etc.) |
| `USCoreNameGenerationStrategy` | Uses race from profile attributes to select appropriate Bogus locale (e.g., `zh_CN` for Asian-Chinese, `es_MX` for Hispanic) |
| `AUBaseNameGenerationStrategy` | Uses `en` (English) locale for Australian names |

---

## Code Constants & Terminology

### FhirCode Record

**File**: `src/Core/Ignixa.FhirFakes/Scenarios/Codes/FhirCode.cs`

```csharp
public record FhirCode(string System, string Code, string Display)
{
    public static class Systems
    {
        public const string SnomedCt = "http://snomed.info/sct";
        public const string Loinc = "http://loinc.org";
        public const string RxNorm = "http://www.nlm.nih.gov/research/umls/rxnorm";
        public const string Cvx = "http://hl7.org/fhir/sid/cvx";
        public const string Icd10 = "http://hl7.org/fhir/sid/icd-10";
        public const string Ucum = "http://unitsofmeasure.org";
    }
}
```

### SNOMED CT - Conditions

| Code | Display |
|------|---------|
| `44054006` | Diabetes mellitus type 2 |
| `714628002` | Prediabetes |
| `38341003` | Hypertensive disorder |
| `59621000` | Essential hypertension |
| `55822004` | Hyperlipidemia |
| `414915002` | Obesity |
| `77386006` | Pregnancy |
| `195967001` | Asthma |
| `68566005` | Urinary tract infectious disease |
| `74400008` | Appendicitis |

### LOINC - Vital Signs

| Code | Display |
|------|---------|
| `8480-6` | Systolic blood pressure |
| `8462-4` | Diastolic blood pressure |
| `85354-9` | Blood pressure panel |
| `29463-7` | Body weight |
| `8302-2` | Body height |
| `39156-5` | Body mass index (BMI) |
| `8310-5` | Body temperature |
| `72514-3` | Pain severity - 0-10 verbal numeric rating |

### LOINC - Lab Observations

| Code | Display |
|------|---------|
| `4548-4` | Hemoglobin A1c |
| `2339-0` | Glucose [Mass/volume] in Blood |
| `2093-3` | Cholesterol [Mass/volume] in Serum or Plasma |
| `2571-8` | Triglyceride [Mass/volume] in Serum or Plasma |
| `2085-9` | Cholesterol in HDL [Mass/volume] in Serum or Plasma |
| `2089-1` | Cholesterol in LDL [Mass/volume] in Serum or Plasma |
| `6690-2` | Leukocytes [#/volume] in Blood by Automated count |
| `718-7` | Hemoglobin [Mass/volume] in Blood |
| `2951-2` | Sodium [Moles/volume] in Serum or Plasma |
| `2823-3` | Potassium [Moles/volume] in Serum or Plasma |
| `2160-0` | Creatinine [Mass/volume] in Serum or Plasma |

### RxNorm - Medications

**Diabetes:**
| Code | Display |
|------|---------|
| `860975` | Metformin hydrochloride 500 MG Oral Tablet |
| `861007` | Metformin hydrochloride 1000 MG Oral Tablet |
| `261551` | Insulin glargine 100 UNT/ML Injectable Solution |

**Hypertension & Cardiovascular:**
| Code | Display |
|------|---------|
| `314076` | Lisinopril 10 MG Oral Tablet |
| `329528` | Amlodipine 5 MG Oral Tablet |
| `617318` | Atorvastatin 20 MG Oral Tablet |
| `243670` | Aspirin 81 MG Oral Tablet |
| `309362` | Clopidogrel 75 MG Oral Tablet |

**Respiratory:**
| Code | Display |
|------|---------|
| `435` | Albuterol 0.083 MG/ML Inhalation Solution |
| `746030` | Fluticasone propionate 0.05 MG/ACTUAT Metered Dose Inhaler |

### Additional Code Files

| File | Contents |
|------|----------|
| `Allergens.cs` | 40+ allergen codes (SNOMED CT) |
| `Immunizations.cs` | 25+ vaccine codes (CVX) |
| `Procedures.cs` | 30+ procedure codes (SNOMED CT, CPT) |
| `DiagnosticReports.cs` | 20+ diagnostic report codes (LOINC) |
| `ServiceRequestCodes.cs` | 20+ service request codes (LOINC, SNOMED) |
| `Specialties.cs` | Medical specialty taxonomy codes |

---

## CLI Tool

The `ignixa-fakes` tool generates FHIR test data from the command line.

### Installation

```bash
dotnet tool install --global Ignixa.FhirFakes.Cli
```

### Command Structure

```
ignixa-fakes [version] [command] [args]

Versions: stu3, r4, r4b, r5, r6
Commands: resource, scenario, population, help
```

### Scenario Command

Generate predefined clinical scenarios as transaction bundles:

```bash
# Generate a diabetic patient scenario
ignixa-fakes r4 scenario DiabeticPatient --out ./output

# Generate with resolved references (batch bundle)
ignixa-fakes r4 scenario HypertensivePatient --out ./output --resolved-references

# Validate generated resources against schema
ignixa-fakes r4 scenario WellnessVisit --out ./output --validate

# List available scenarios
ignixa-fakes help scenarios
```

**Output**: `{version}-bundle-{scenario}-{guid}.json`

### Population Command

Generate realistic patient populations:

```bash
# Generate 100 patients from Massachusetts
ignixa-fakes r4 population --from Massachusetts --count 100 --out ./output

# Generate as separate batch bundles (one per patient)
ignixa-fakes r4 population --from Boston --count 50 --out ./output --resolved-references

# Generate as NDJSON files (one file per resource type)
ignixa-fakes r4 population --from California --count 1000 --out ./output --ndjson
```

**Output Formats:**

| Option | Output |
|--------|--------|
| (default) | Single transaction bundle: `{version}-bundle-population-{state}-{count}-{guid}.json` |
| `--resolved-references` | Multiple batch bundles: `{version}-bundle-population-{state}-{count}-{n}-{guid}.json` |
| `--ndjson` | Per-resource-type files: `{version}-population-{state}-{type}-{count}-{guid}.ndjson` |

### Resource Command

Generate random resources based on schema:

```bash
# Generate a random Patient resource
ignixa-fakes r4 resource Patient --out ./output

# Generate an Observation using a predefined state
ignixa-fakes r4 resource Observation BloodGlucose --out ./output
```

### Built-in Help

```bash
ignixa-fakes help              # General help
ignixa-fakes help scenarios    # List all available scenarios
ignixa-fakes help states       # List all available observation states
ignixa-fakes help cities       # List all available cities
ignixa-fakes help versions     # Show supported FHIR versions
```

---

## Testing & Validation Features

### Tag System for Test Isolation

All builders support `.WithTag(string tag)` which adds a `meta.tag` entry:

```csharp
var testTag = Guid.NewGuid().ToString();
var scenario = new ScenarioBuilder(schemaProvider)
    .WithTag(testTag)
    .WithPatient(p => p.WithAge(40))
    .AddEncounter("Visit")
    .Build();

// All resources tagged with: system="http://ignixa.dev/test-isolation", code=testTag
// Search: GET /Patient?_tag={testTag}
```

### Schema Validation

The CLI includes a `--validate` flag that validates generated resources against the FHIR schema using `Ignixa.Validation`:

- Resource structure validation
- Cardinality constraints
- Required element presence
- Data type correctness
- Code system validity (where applicable)

### Test Projects

| Test Project | Coverage |
|-------------|----------|
| `Ignixa.FhirFakes.Tests` | Core library tests — binding-aware generation, cross-version compatibility, builder-specific tests |
| `Ignixa.FhirFakes.Cli.Tests` | CLI tests — command parsing, output format validation |

**Key Test Files:**
- `ComprehensiveValidationTests.cs` — Validates generated resources against FHIR schema
- `BindingAwareGenerationTests.cs` — Tests binding-aware code generation
- `CrossVersionCompatibilityTests.cs` — Tests R4/R5/STU3 compatibility

---

## Integration & Dependencies

### NuGet Dependencies

**FhirFakes.csproj:**
```xml
<PackageReference Include="Bogus" />
<ProjectReference Include="../Ignixa.Specification/Ignixa.Specification.csproj" />
<ProjectReference Include="../Ignixa.Serialization/Ignixa.Serialization.csproj" />
```

**Cli.csproj:**
```xml
<PackageReference Include="System.CommandLine" />
<ProjectReference Include="../../src/Core/Ignixa.FhirFakes/Ignixa.FhirFakes.csproj" />
```

### External Dependencies

| Package | Purpose |
|---------|---------|
| **Bogus** | Faker library for realistic fake data generation (names, addresses, dates) |
| **System.CommandLine** | CLI framework for command-line parsing |
| **Ignixa.Specification** | FHIR schema provider (R4, R5, STU3, R4B, R6) |
| **Ignixa.Serialization** | FHIR JSON serialization/deserialization |
| **Ignixa.Validation** | FHIR resource validation (CLI only) |

### FHIR Version Support

The library supports all major FHIR versions through `IFhirSchemaProvider`:
- **STU3** (`STU3CoreSchemaProvider`)
- **R4** (`R4CoreSchemaProvider`)
- **R4B** (`R4BCoreSchemaProvider`)
- **R5** (`R5CoreSchemaProvider`)
- **R6** (`R6CoreSchemaProvider`)

### NuGet Statistics

| Package | Downloads |
|---------|-----------|
| `Ignixa.FhirFakes` | ~4,644 |
| `Ignixa.FhirFakes.Cli` | ~2,603 |

---

## Ecosystem Position & Comparisons

### Synthetic Data Generator Landscape

| Tool | Language | Type | Best For |
|------|----------|------|----------|
| **Ignixa.FhirFakes** | **.NET 9 / C#** | **Schema-based, scenario-driven** | **.NET testing, clinical scenarios, CI pipelines** |
| Synthea | Java | Population simulator | Population-level analytics, load testing |
| Verial | Unknown | Scenario-driven (paid) | AI agent testing, vendor-specific shapes |
| Tonic.ai | Multi | Data synthesis from real | De-identified synthetic clones of production |
| Firely YamlGen | .NET | Template-based | Small-scale test resource generation |

### Ignixa.FhirFakes vs. Synthea

| Dimension | Ignixa.FhirFakes | Synthea |
|-----------|-----------------|---------|
| **Language** | .NET 9 / C# | Java |
| **Architecture** | 4-layer: Random → Builders → Scenarios → Populations | Module-based disease progression |
| **Scenario Control** | Yes (`ScenarioBuilder`, predefined scenarios) | Limited (probabilistic only) |
| **FHIR Versions** | STU3, R4, R4B, R5, R6 | Primarily R4 |
| **Output Formats** | JSON Bundle, NDJSON | JSON Bundle, CSV, FHIR |
| **CLI** | Yes (`ignixa-fakes`) | Yes (`run_synthea`) |
| **Schema Awareness** | Yes — respects FHIR profiles and bindings | Yes — valid FHIR output |
| **Population Scale** | Moderate (city-based demographics) | Excellent (millions of patients) |
| **.NET Integration** | **Native** | Requires Java runtime |

### Key Differentiators

1. **Schema-Aware, Binding-Respectful Generation**: Uses `Ignixa.Specification` to ensure generated data respects FHIR profiles and terminology bindings, producing clinically coherent resources rather than random JSON
2. **4-Layer Architecture**: From random resources → clinical scenarios → patient lifecycles → populations, offering more control than purely probabilistic generation
3. **US Census-Based Demographics**: `CityDemographics` with real distributions for 11 major US cities, powered by `Bogus` locale-aware name generation for culturally appropriate names
4. **Scenario-Driven (vs. Population-Driven)**: Predefined scenarios like `DiabeticPatient`, `UrinaryTractInfection`, `AsthmaticChild` make it suitable for **workflow-specific testing** rather than just population analytics
5. **.NET-Native**: No Java/Gradle dependency — integrates directly into .NET test suites and CI pipelines with seamless debugging

---

## Getting Started Examples

### Example 1: Generate a Random Patient

```csharp
using Ignixa.FhirFakes;
using Ignixa.Specification;

var schemaProvider = FhirVersion.R4.GetSchemaProvider();
var faker = new SchemaBasedFhirResourceFaker(schemaProvider);

// Generate a random Patient resource
var patient = faker.Generate("Patient");

// Generate with a tag for test isolation
faker.WithTag("test-run-123");
var taggedPatient = faker.Generate("Patient");
```

### Example 2: Use the Patient Builder

```csharp
using Ignixa.FhirFakes.Builders;

// Simple patient with manual demographics
var patient = PatientBuilderFactory.Create(schemaProvider)
    .WithAge(45)
    .WithGender(g => g.Male)
    .WithGivenName("John")
    .WithFamilyName("Smith")
    .Build();

// Realistic patient from specific city
var realisticPatient = PatientBuilderFactory.Create(schemaProvider)
    .FromCity(KnownCities.Boston)
    .WithAge(45)
    .WithRealisticBMI()
    .Build();
```

### Example 3: Build a Clinical Scenario

```csharp
using Ignixa.FhirFakes.Scenarios;
using Ignixa.FhirFakes.Scenarios.Codes;

var scenario = new ScenarioBuilder(schemaProvider)
    .WithName("Hypertension Screening")
    .WithPatient(p => p
        .WithAge(55)
        .WithGender(g => g.Male))
    .AddEncounter("Annual checkup")
    .AddObservation(VitalSigns.BloodPressureSystolic, 140m, "mmHg")
    .AddConditionOnset(FhirCode.Conditions.HypertensionEssential)
    .Build();

var patient = scenario.Patient;
var bundle = scenario.ToBundle();
```

### Example 4: Use a Predefined Scenario

```csharp
using Ignixa.FhirFakes.Scenarios.Predefined;

var scenario = schemaProvider.GetDiabeticPatient(
    age: 52,
    gender: "male",
    severity: 2);

var bundle = scenario.ToBundle();
```

### Example 5: Generate a Population

```csharp
using Ignixa.FhirFakes.Population;

var generator = new PopulationGenerator(schemaProvider);

// Generate 1000 patients from Massachusetts
foreach (var scenario in generator.Generate("Massachusetts", 1000))
{
    var bundle = scenario.ToBundle();
    // Post to FHIR server or save to file
}
```

### Example 6: Export to NDJSON

```csharp
var generator = new PopulationGenerator(schemaProvider);

await using var writer = File.CreateText("population.ndjson");

foreach (var scenario in generator.Generate("California", 100))
{
    foreach (var resource in scenario.AllResources)
    {
        var json = resource.SerializeToString();
        await writer.WriteLineAsync(json);
    }
}
```

### Example 7: Test Isolation with Tags

```csharp
var testTag = Guid.NewGuid().ToString();

var scenario = new ScenarioBuilder(schemaProvider)
    .WithTag(testTag)
    .WithPatient(p => p.WithAge(40))
    .AddEncounter("Visit")
    .Build();

var bundle = scenario.ToBundle();

// All resources in the bundle are tagged
// Search with: GET /Patient?_tag={testTag}
```

---

## Key Design Patterns

| Pattern | Used In | Purpose |
|---------|---------|---------|
| **CRTP** | `FhirResourceBuilder<TBuilder>` | Enables fluent APIs where base class methods return the derived builder type |
| **Builder** | All `*Builder` classes | Fluent, chainable configuration before `Build()` |
| **Factory** | `PatientBuilderFactory`, `PatientProfileFactory` | Lazy-initialized singletons for expensive resources |
| **State Machine** | `ScenarioBuilder`, `ScenarioState` | Each scenario step is executed sequentially |
| **Strategy** | `INameGenerationStrategy`, `IPatientProfile` | Different implementations for different countries/regions |
| **Attributes Dictionary** | `IPatientProfile` | Extensibility without interface changes |
| **Lazy Reflection Caching** | `BindingCodeMapper` | Static code arrays cached lazily via reflection |
| **Extension Methods** | `PredefinedScenarios` | All 14 scenarios are extension methods on `IFhirSchemaProvider` |

---

## References

- **GitHub Repository**: `https://github.com/brendankowitz/ignixa-fhir`
- **Documentation**: `https://brendankowitz.github.io/ignixa-fhir/docs/core-sdk/fhir-fakes`
- **NuGet Package**: `https://www.nuget.org/packages/Ignixa.FhirFakes`
- **NuGet CLI Tool**: `https://www.nuget.org/packages/Ignixa.FhirFakes.Cli`
- **Synthea (Reference)**: `https://synthetichealth.github.io/synthea/`
