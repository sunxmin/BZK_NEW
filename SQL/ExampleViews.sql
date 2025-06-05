-- 专病多维度查询系统 - 示例视图定义
-- 这些示例视图展示了如何实现复杂的多表关联查询

-- 1. 表A去重后与表B关联，取B表最新记录的视图
-- 假设有患者表(Patients)和检查记录表(Exams)，我们需要患者去重后与最新的检查记录关联
CREATE VIEW vw_PatientLatestExam AS
WITH DistinctPatients AS (
    -- 对患者表去重，只保留唯一记录
    SELECT DISTINCT PatientID, Name, Gender, BirthDate, Address
    FROM PatientInfo
),
LatestExams AS (
    -- 使用ROW_NUMBER()窗口函数，为每个患者的检查按日期排序
    SELECT *,
        ROW_NUMBER() OVER (PARTITION BY PatientID ORDER BY ExamDate DESC) AS RowNum
    FROM ExamRecord
)
-- 关联查询
SELECT 
    p.PatientID, p.Name, p.Gender, p.BirthDate, p.Address,
    e.ExamID, e.ExamDate, e.ExamType, e.Result, e.DoctorName
FROM DistinctPatients p
LEFT JOIN LatestExams e ON p.PatientID = e.PatientID AND e.RowNum = 1;

-- 2. 按科室和疾病分组统计的患者人数
CREATE VIEW vw_DiseaseStatsByDept AS
SELECT 
    d.DepartmentName,
    di.DiseaseName,
    COUNT(p.PatientID) AS PatientCount,
    AVG(DATEDIFF(DAY, p.AdmissionDate, p.DischargeDate)) AS AvgStayDays,
    MIN(p.AdmissionDate) AS EarliestAdmission,
    MAX(p.DischargeDate) AS LatestDischarge
FROM PatientVisit p
JOIN Department d ON p.DepartmentID = d.DepartmentID
JOIN Disease di ON p.DiseaseID = di.DiseaseID
GROUP BY d.DepartmentName, di.DiseaseName;

-- 3. 按年龄段统计的疾病发病率
CREATE VIEW vw_DiseaseByAgeGroup AS
WITH PatientAgeGroups AS (
    SELECT 
        PatientID,
        CASE 
            WHEN DATEDIFF(YEAR, BirthDate, GETDATE()) < 18 THEN '0-17'
            WHEN DATEDIFF(YEAR, BirthDate, GETDATE()) BETWEEN 18 AND 35 THEN '18-35'
            WHEN DATEDIFF(YEAR, BirthDate, GETDATE()) BETWEEN 36 AND 50 THEN '36-50'
            WHEN DATEDIFF(YEAR, BirthDate, GETDATE()) BETWEEN 51 AND 65 THEN '51-65'
            ELSE '65+' 
        END AS AgeGroup
    FROM PatientInfo
)
SELECT 
    d.DiseaseName,
    p.AgeGroup,
    COUNT(v.PatientID) AS PatientCount,
    COUNT(v.PatientID) * 100.0 / SUM(COUNT(v.PatientID)) OVER (PARTITION BY d.DiseaseName) AS PercentageInDisease
FROM PatientVisit v
JOIN Disease d ON v.DiseaseID = d.DiseaseID
JOIN PatientAgeGroups p ON v.PatientID = p.PatientID
GROUP BY d.DiseaseName, p.AgeGroup;

-- 4. 组合多个表的统计视图 - 患者检查结果异常分析
CREATE VIEW vw_AbnormalExamAnalysis AS
WITH PatientExams AS (
    -- 获取所有带有异常标记的检查
    SELECT 
        p.PatientID,
        p.Name,
        p.Gender,
        p.BirthDate,
        e.ExamID,
        e.ExamDate,
        e.ExamType,
        e.Result,
        CASE WHEN e.Result LIKE '%异常%' THEN 1 ELSE 0 END AS IsAbnormal
    FROM PatientInfo p
    JOIN ExamRecord e ON p.PatientID = e.PatientID
),
PatientDiagnosis AS (
    -- 获取患者诊断信息
    SELECT 
        v.PatientID,
        d.DiseaseName,
        v.DiagnosisDate,
        ROW_NUMBER() OVER (PARTITION BY v.PatientID ORDER BY v.DiagnosisDate DESC) AS DiagnosisRank
    FROM PatientVisit v
    JOIN Disease d ON v.DiseaseID = d.DiseaseID
)
-- 关联查询，显示患者最近诊断和异常检查结果
SELECT 
    e.PatientID,
    e.Name,
    e.Gender,
    DATEDIFF(YEAR, e.BirthDate, GETDATE()) AS Age,
    e.ExamType,
    e.ExamDate,
    e.Result,
    d.DiseaseName AS LatestDiagnosis,
    d.DiagnosisDate
FROM PatientExams e
LEFT JOIN PatientDiagnosis d ON e.PatientID = d.PatientID AND d.DiagnosisRank = 1
WHERE e.IsAbnormal = 1
ORDER BY e.ExamDate DESC;

-- 5. 筛选数据后分组聚合的视图
CREATE VIEW vw_TreatmentEffectiveness AS
WITH TreatmentResults AS (
    -- 筛选有效的治疗记录
    SELECT 
        t.PatientID,
        t.TreatmentType,
        t.StartDate,
        t.EndDate,
        d.DiseaseName,
        CASE 
            WHEN t.Effectiveness = 'Good' THEN 3
            WHEN t.Effectiveness = 'Moderate' THEN 2
            WHEN t.Effectiveness = 'Poor' THEN 1
            ELSE 0 
        END AS EffectivenessScore
    FROM Treatment t
    JOIN PatientVisit v ON t.PatientID = v.PatientID
    JOIN Disease d ON v.DiseaseID = d.DiseaseID
    WHERE t.EndDate IS NOT NULL -- 只包含已完成的治疗
    AND t.Effectiveness IS NOT NULL -- 只包含有评估结果的治疗
)
-- 按疾病和治疗类型统计有效性
SELECT 
    DiseaseName,
    TreatmentType,
    COUNT(*) AS TreatmentCount,
    AVG(EffectivenessScore) AS AvgEffectiveness,
    SUM(CASE WHEN EffectivenessScore >= 3 THEN 1 ELSE 0 END) AS GoodOutcomes,
    SUM(CASE WHEN EffectivenessScore = 2 THEN 1 ELSE 0 END) AS ModerateOutcomes,
    SUM(CASE WHEN EffectivenessScore <= 1 THEN 1 ELSE 0 END) AS PoorOutcomes
FROM TreatmentResults
GROUP BY DiseaseName, TreatmentType
HAVING COUNT(*) >= 5; -- 只包含样本量足够的组合 