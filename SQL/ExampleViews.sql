-- ר����ά�Ȳ�ѯϵͳ - ʾ����ͼ����
-- ��Щʾ����ͼչʾ�����ʵ�ָ��ӵĶ�������ѯ

-- 1. ��Aȥ�غ����B������ȡB�����¼�¼����ͼ
-- �����л��߱�(Patients)�ͼ���¼��(Exams)��������Ҫ����ȥ�غ������µļ���¼����
CREATE VIEW vw_PatientLatestExam AS
WITH DistinctPatients AS (
    -- �Ի��߱�ȥ�أ�ֻ����Ψһ��¼
    SELECT DISTINCT PatientID, Name, Gender, BirthDate, Address
    FROM PatientInfo
),
LatestExams AS (
    -- ʹ��ROW_NUMBER()���ں�����Ϊÿ�����ߵļ�鰴��������
    SELECT *,
        ROW_NUMBER() OVER (PARTITION BY PatientID ORDER BY ExamDate DESC) AS RowNum
    FROM ExamRecord
)
-- ������ѯ
SELECT 
    p.PatientID, p.Name, p.Gender, p.BirthDate, p.Address,
    e.ExamID, e.ExamDate, e.ExamType, e.Result, e.DoctorName
FROM DistinctPatients p
LEFT JOIN LatestExams e ON p.PatientID = e.PatientID AND e.RowNum = 1;

-- 2. �����Һͼ�������ͳ�ƵĻ�������
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

-- 3. �������ͳ�Ƶļ���������
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

-- 4. ��϶�����ͳ����ͼ - ���߼�����쳣����
CREATE VIEW vw_AbnormalExamAnalysis AS
WITH PatientExams AS (
    -- ��ȡ���д����쳣��ǵļ��
    SELECT 
        p.PatientID,
        p.Name,
        p.Gender,
        p.BirthDate,
        e.ExamID,
        e.ExamDate,
        e.ExamType,
        e.Result,
        CASE WHEN e.Result LIKE '%�쳣%' THEN 1 ELSE 0 END AS IsAbnormal
    FROM PatientInfo p
    JOIN ExamRecord e ON p.PatientID = e.PatientID
),
PatientDiagnosis AS (
    -- ��ȡ���������Ϣ
    SELECT 
        v.PatientID,
        d.DiseaseName,
        v.DiagnosisDate,
        ROW_NUMBER() OVER (PARTITION BY v.PatientID ORDER BY v.DiagnosisDate DESC) AS DiagnosisRank
    FROM PatientVisit v
    JOIN Disease d ON v.DiseaseID = d.DiseaseID
)
-- ������ѯ����ʾ���������Ϻ��쳣�����
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

-- 5. ɸѡ���ݺ����ۺϵ���ͼ
CREATE VIEW vw_TreatmentEffectiveness AS
WITH TreatmentResults AS (
    -- ɸѡ��Ч�����Ƽ�¼
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
    WHERE t.EndDate IS NOT NULL -- ֻ��������ɵ�����
    AND t.Effectiveness IS NOT NULL -- ֻ�������������������
)
-- ����������������ͳ����Ч��
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
HAVING COUNT(*) >= 5; -- ֻ�����������㹻����� 