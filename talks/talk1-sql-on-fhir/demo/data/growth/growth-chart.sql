-- Growth chart — height-for-age across the pediatric cohort.
--
-- Patient + height Observations, flattened by SQL on FHIR to Parquet, joined and aggregated
-- by age. The median height rises smoothly with age: a population growth curve straight out
-- of FHIR, no ETL.
--
-- Run from this folder once the Parquet views exist in ./output/:
--   duckdb -c ".read growth-chart.sql"

WITH heights AS (
    SELECT
        date_part('year', o.test_date) - date_part('year', p.birth_date) AS age_years,
        o.value AS height_cm
    FROM 'output/observation-view.parquet' AS o
    JOIN 'output/patient-view.parquet'     AS p
          ON p.patient_id = o.patient_id
    WHERE o.loinc_code = '8302-2'          -- LOINC: body height
)
SELECT
    age_years,
    count(*)                       AS measurements,
    round(median(height_cm), 1)    AS median_height_cm,
    round(min(height_cm), 1)       AS shortest_cm,
    round(max(height_cm), 1)       AS tallest_cm
FROM heights
WHERE age_years BETWEEN 1 AND 18
GROUP BY age_years
ORDER BY age_years;
