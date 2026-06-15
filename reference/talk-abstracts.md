# Flattening the Curve: Implementing the SQL on FHIR ViewDefinition in .NET

The emerging SQL on FHIR specification takes great strides to solve the "nested data" problem for analytics, but we need a variety of robust tooling for all ecosystems. To bridge this gap for the .NET ecosystem, this session looks at building an open-source project featuring a native parser, evaluator, and CLI that passes the official SQL on FHIR conformance test suite.

This session is a technical retrospective on implementing a complex FHIR standard from scratch. Beyond just spec compliance, we will explore how a high-performance .NET native engine enables modern data pipelines to export into a Data Lake in Parquet format, simplifying and accelerating downstream data processing.

# Fluent, Fast, and Fake: .NET Synthetic FHIR Data for Developer Happiness

Every FHIR developer faces the same hurdle: getting good test data. We often rely on the robust, industry-standard Synthea for population simulations, or we fall back to hand-crafting or example JSON files for testing. But for .NET developers building high-velocity applications, neither option is ideal. We need data that is lightweight, deterministic, and tightly integrated into our test suites.

We introduce a native .NET generator prioritizing developer experience. Explore a 4-layer architecture, from random schema-based generation to fluent builders, expressing clinical intent in fluent C#. Learn to create deterministic test data or use the CLI to spawn localized cohorts (e.g. "1,000 patients from Seattle") for pipeline stress-testing.

# Bio

Originally from Australia, Brendan has been in the beautiful Pacific Northwest of the U.S. for the past several years working at Microsoft. Now an Engineering Manager, Brendan leads a team that develops the open source and hosted FHIR Server for Azure applications.