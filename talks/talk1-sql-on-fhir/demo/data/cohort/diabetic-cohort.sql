-- Diabetic care registry — the payoff query.
--
-- Three FHIR resource types, flattened by SQL on FHIR into Parquet, joined back
-- together with plain SQL: which diagnosed diabetics are above the 7% HbA1c target,
-- and which way is their control trending over the last three labs?
--
-- Run from this folder once the Parquet views exist in ./output/:
--   duckdb -c ".read diabetic-cohort.sql"

WITH a1c AS (
    SELECT
        patient_id,
        first_value(hba1c_value) OVER w AS first_a1c,
        last_value(hba1c_value)  OVER w AS latest_a1c,
        row_number()             OVER w AS rn
    FROM 'output/observation-view.parquet'
    WINDOW w AS (
        PARTITION BY patient_id ORDER BY test_date
        ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING
    )
)
SELECT
    p.given_name || ' ' || p.family_name              AS patient,
    round(a1c.first_a1c, 1)                           AS first_hba1c,
    round(a1c.latest_a1c, 1)                          AS latest_hba1c,
    round(a1c.latest_a1c - a1c.first_a1c, 1)          AS change,
    CASE WHEN a1c.latest_a1c > a1c.first_a1c
         THEN 'worsening' ELSE 'improving' END        AS trend,
    CASE WHEN a1c.latest_a1c > 7.0
         THEN 'ABOVE target' ELSE 'at target' END     AS status
FROM 'output/patient-view.parquet'      AS p
JOIN 'output/condition-view.parquet'    AS c
      ON c.patient_id = p.patient_id
     AND c.code = '44054006'            -- SNOMED: Diabetes mellitus type 2
JOIN a1c
      ON a1c.patient_id = p.patient_id
     AND a1c.rn = 1
ORDER BY a1c.latest_a1c DESC;
