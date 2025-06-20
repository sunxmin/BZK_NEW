USE [ZBKQuerySystem]
GO
/****** Object:  Table [dbo].[__EFMigrationsHistory]    Script Date: 2025/5/30 9:50:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[__EFMigrationsHistory](
	[MigrationId] [nvarchar](150) NOT NULL,
	[ProductVersion] [nvarchar](32) NOT NULL,
 CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY CLUSTERED 
(
	[MigrationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[AllowedTables]    Script Date: 2025/5/30 9:50:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AllowedTables](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [nvarchar](450) NOT NULL,
	[TableName] [nvarchar](128) NOT NULL,
	[CanRead] [bit] NOT NULL,
	[CanWrite] [bit] NOT NULL,
	[CanExport] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[ColumnInfos]    Script Date: 2025/5/30 9:50:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ColumnInfos](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[TableId] [int] NOT NULL,
	[ColumnName] [nvarchar](128) NOT NULL,
	[DisplayName] [nvarchar](128) NOT NULL,
	[DataType] [nvarchar](50) NOT NULL,
	[IsPrimaryKey] [bit] NOT NULL,
	[IsNullable] [bit] NOT NULL,
	[Description] [nvarchar](500) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[QueryShares]    Script Date: 2025/5/30 9:50:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[QueryShares](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[QueryId] [int] NOT NULL,
	[UserId] [nvarchar](450) NOT NULL,
	[SharedAt] [datetime2](7) NOT NULL,
	[SharedBy] [nvarchar](450) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[RoleClaims]    Script Date: 2025/5/30 9:50:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[RoleClaims](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[RoleId] [nvarchar](450) NOT NULL,
	[ClaimType] [nvarchar](max) NOT NULL,
	[ClaimValue] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_RoleClaims] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  Table [dbo].[Roles]    Script Date: 2025/5/30 9:50:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Roles](
	[Id] [nvarchar](450) NOT NULL,
	[Name] [nvarchar](450) NOT NULL,
	[NormalizedName] [nvarchar](max) NOT NULL,
	[ConcurrencyStamp] [nvarchar](max) NOT NULL,
	[Description] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_Roles] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  Table [dbo].[SavedQueries]    Script Date: 2025/5/30 9:50:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SavedQueries](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [nvarchar](450) NOT NULL,
	[Name] [nvarchar](128) NOT NULL,
	[Description] [nvarchar](500) NULL,
	[SqlQuery] [nvarchar](max) NOT NULL,
	[TablesIncluded] [nvarchar](max) NULL,
	[ColumnsIncluded] [nvarchar](max) NULL,
	[FilterConditions] [nvarchar](max) NULL,
	[SortOrder] [nvarchar](max) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NOT NULL,
	[CreatedBy] [nvarchar](450) NULL,
	[IsShared] [bit] NOT NULL,
	[JoinConditions] [nvarchar](max) NULL,
	[IsCustomSql] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  Table [dbo].[TableInfos]    Script Date: 2025/5/30 9:50:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TableInfos](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[TableName] [nvarchar](128) NOT NULL,
	[DisplayName] [nvarchar](128) NOT NULL,
	[Description] [nvarchar](500) NULL,
	[IsView] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[UserRoles]    Script Date: 2025/5/30 9:50:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UserRoles](
	[UserId] [nvarchar](450) NOT NULL,
	[RoleId] [nvarchar](450) NOT NULL,
 CONSTRAINT [PK_UserRoles] PRIMARY KEY CLUSTERED 
(
	[UserId] ASC,
	[RoleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[Users]    Script Date: 2025/5/30 9:50:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Users](
	[Id] [nvarchar](450) NOT NULL,
	[UserName] [nvarchar](450) NOT NULL,
	[NormalizedUserName] [nvarchar](max) NOT NULL,
	[Email] [nvarchar](450) NOT NULL,
	[NormalizedEmail] [nvarchar](max) NOT NULL,
	[EmailConfirmed] [bit] NOT NULL,
	[PasswordHash] [nvarchar](max) NOT NULL,
	[SecurityStamp] [nvarchar](max) NOT NULL,
	[ConcurrencyStamp] [nvarchar](max) NOT NULL,
	[PhoneNumber] [nvarchar](max) NOT NULL,
	[PhoneNumberConfirmed] [bit] NOT NULL,
	[TwoFactorEnabled] [bit] NOT NULL,
	[LockoutEnd] [datetimeoffset](7) NULL,
	[LockoutEnabled] [bit] NOT NULL,
	[AccessFailedCount] [int] NOT NULL,
	[DisplayName] [nvarchar](max) NOT NULL,
	[Department] [nvarchar](max) NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[LastLogin] [datetime2](7) NOT NULL,
	[IsActive] [bit] NOT NULL,
 CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  View [dbo].[01_患者信息]    Script Date: 2025/5/30 9:50:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO



CREATE view [dbo].[01_患者信息]
as 
select 
EMPI 患者主索引 ,
jzlb 就诊类别,
patid 患者编号,
hzxm 患者姓名,
sfzh 身份证号,
sex 患者性别, 
birthday  生日,
hyzt 婚姻状况 , 
blh 病历号,
CardNo 卡号,
ybdm 医保类型,
lxdz 联系地址,
lxdh 联系电话,
djrq 登记日期
from [ICD_RXA].dbo.Fact_PatientInfo (nolock)






GO
/****** Object:  View [dbo].[02_检查结果]    Script Date: 2025/5/30 9:50:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO



---根据数据模型说明创建视图
create view [dbo].[02_检查结果]
as 
select
EMPI	 患者主索引
,jzlb	 就诊类别 
,patid	 患者编号 
,jzlsh	 患者就诊唯一编号 
,hzxm	 患者姓名
,ApplyNo	 	申请单
,ApplyTime	 	申请时间
,ExamTime	 	检查时间
,ReportTime	 	报告时间
,PublicTime	 	发布时间
,ItemCode	 	项目代码
,ItemName	 	项目名称
,bw	 	部位
,jcsj	 检查所见
,jcjl	 检查结论
,TechNo	 影像号
,Status	 状态 
 --JCType	 检查类型（超声、放射等，也可用于区分数据来源）
,ApplyDept	 	申请科室
,ApplyDeptName 	申请科室名称
from [ICD_RXA].dbo.[Fact_RisResult] (nolock)




GO
/****** Object:  View [dbo].[03_检验结果]    Script Date: 2025/5/30 9:50:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


---根据数据模型说明创建视图
create view [dbo].[03_检验结果]
as 
select
EMPI	患者主索引,
jzlb	就诊类别,
patid	患者编号,
jzlsh	患者就诊唯一编号,
hzxm	患者姓名,
ApplyNo	申请单,
SampleName	样本名称,
ApplyTime	申请时间,
SampleTime	采样时间,
ReceiveTime	收到日期,
ExecTime	检验日期,
ReportTime	报告日期,
ResultTime	结果日期,
ExamCode	项目代码,
ExamName	项目名称,
ItemCode	指标代码,
ItemName	指标名称,
Result	结果,
HighLowFlag	标识,
ReferenceRange	参考值,
Unit	单位,
Status	状态,
ApplyDept	申请科室,
ApplyDeptName	申请科室名称,
SerialNo	结果表编号







from [ICD_RXA].dbo.Fact_LisResult (nolock)



GO
/****** Object:  View [dbo].[04_病理检查结果]    Script Date: 2025/5/30 9:50:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO



---根据数据模型说明创建视图
CREATE view [dbo].[04_病理检查结果]
as 
select
EMPI	患者主索引,
jzlb	就诊类别,
patid	患者编号,
jzlsh	患者就诊唯一编号,
hzxm	患者姓名,
ApplyNo		报告单号,
Age	年龄,
ApplyDept 申请科室,
ApplyDeptName	申请科室名称,
ApplyTime	申请时间,
AcceptTime	接收时间,
ReportTime	报告时间,
PublicTime	发布时间,
TechNo 病理号,
ClinicDesc		临床诊断,
InstName	病理报告名称,
jcjl	病理诊断,
StatusName		状态,
 sex 性别

from [ICD_RXA].dbo.Fact_BLJCXX (nolock)




GO
/****** Object:  View [dbo].[05_细菌鉴定结果]    Script Date: 2025/5/30 9:50:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


---根据数据模型说明创建视图
create view [dbo].[05_细菌鉴定结果]
as 
select
EMPI	患者主索引,
jzlb	就诊类别,
patid	患者编号,
jzlsh	患者就诊唯一编号,
hzxm	患者姓名,
AppraisalOrganismId	细菌编号,
ApplyNo	申请单号,
AppraisalId		鉴定编号,
OrganismCode	细菌代码,
OrganismName	细菌名称,
SampleName	    标本名称,
TestMethodCode	鉴定方法,
ApplyTime	    申请日期,
SampleTime	    采样日期,
ReceiveTime	    接收日期,
PubDateTime	    发布时间,
WarnorganismName	多重耐药菌标志,
Quantity	菌落计数,
ApplyDeptCode	申请科室代码,
ApplyDeptName	申请科室名称,
Status	状态






from [ICD_RXA].dbo.Fact_MISResult (nolock)



GO
/****** Object:  View [dbo].[06_药敏结果]    Script Date: 2025/5/30 9:50:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


---根据数据模型说明创建视图
create view [dbo].[06_药敏结果]
as 
select
EMPI	患者主索引,
jzlb	就诊类别,
patid	患者编号,
jzlsh	患者就诊唯一编号,
hzxm	患者姓名,
AppraisalOrganismId		细菌编号,
ApplyNo	申请单号,
AppraisalId	鉴定编号,
OrganismCode	细菌代码,
OrganismName	细菌名称,
ApplyTime	    申请日期,
SampleTime	    采样日期,
ReceiveTime	    接收日期,
PubDateTime	    发布时间,
WarnorganismName	多重耐药菌标志,
Quantity	菌落计数,
AntiCode	抗生素代码,
AntiName	抗生素名称,
AntiValue	测定值,
AntiResultCode	药敏结果,
ReferValue	参考值,
ApplyDeptCode	申请科室代码,
ApplyDeptName	申请科室名称,
Status	状态,
AppraisalAntiDtlId	药敏编号



from [ICD_RXA].dbo.Fact_MISYMResult (nolock)



GO
/****** Object:  View [dbo].[07_门诊处方明细]    Script Date: 2025/5/30 9:50:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


---根据数据模型说明创建视图
create view [dbo].[07_门诊处方明细]
as 
select
EMPI	患者主索引,
jzlb	就诊类别,
patid	患者编号,
jzlsh	患者就诊唯一编号,
hzxm	患者姓名,
sfzh	身份证号,
jznl	就诊时年龄,
cfrq	处方日期,
ksdm	科室代码,
ksmc	科室名称,
ypdm	药品代码,
lcypdm	临床药品代码,
ggidm	规格代码,
ypmc	药品名称,
ypsl	药品数量,
ypgg	药品规格,
ypdw	单位,
yplb	药品类别,
sfzt	收费状态,
cfxh	处方序号,
cfmxxh	处方明细序号






from [ICD_RXA].dbo.Fact_MZCFMX (nolock)



GO
/****** Object:  View [dbo].[08_住院药品医嘱]    Script Date: 2025/5/30 9:50:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

---根据数据模型说明创建视图
create view [dbo].[08_住院药品医嘱]
as 
select
EMPI 患者主索引,
jzlb 就诊类别,
patid 患者编号,
jzlsh 患者就诊唯一编号,
hzxm 患者姓名,
xh 	医嘱编号 ,
fzxh 分组序号,
ksdm 科室代码,
ksmc 科室名称,
bqdm 病区代码,
bqmc 病区名称,
ksrq 开始日期,
tzrq 停止日期,
shrq 审核日期,
ypdm 药品代码,
lcypdm 临床药品代码,
ggidm 规格代码,
ypmc 药品名称 ,
ypsl 药品数量 ,
ypgg 药品规格 ,
zxdw 执行单位 ,
ypjl 药品剂量 ,
jldw 剂量单位 ,
ypyf 药品用法 ,
yppc 药品频次 ,
yzlb 医嘱类别,
yzlx 医嘱类型,
yzzt 医嘱状态,
yznr 医嘱内容

from [ICD_RXA].dbo.Fact_ZYYPYZ (nolock)



GO
/****** Object:  View [dbo].[09_住院医嘱]    Script Date: 2025/5/30 9:50:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE view [dbo].[09_住院医嘱]
as 
select
EMPI 患者主索引,
jzlb 就诊类别,
patid 患者编号,
jzlsh 患者就诊唯一编号,
hzxm 患者姓名,
xh 医嘱编号,
fzxh 分组序号 ,
ksdm 科室代码 ,
ksmc 科室名称 ,
bqdm 病区代码 ,
bqmc 病区名称 ,
ksrq  开始日期,
tzrq  停止日期,
shrq  审核日期,
ypdm 项目代码,
ypmc 项目名称,
ypsl 项目数量,
ypgg 项目规格,
zxdw 执行单位,
yppc 项目频次,
yzlb 医嘱类别,
yzlx 医嘱类型,
yzzt 医嘱状态,
yznr 医嘱内容


from[ICD_RXA].dbo.Fact_ZYQTYZ (nolock)



GO
/****** Object:  View [dbo].[10_住院病案手术]    Script Date: 2025/5/30 9:50:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE view [dbo].[10_住院病案手术]
as 
select 
EMPI 患者主索引,
jzlb 就诊类别,
patid 患者编号,
jzlsh 患者就诊唯一编号,
hzxm 患者姓名,
sex 性别,
basyxh 病案首页序号,
ssxh 手术序号,
ssrq 手术日期,
ssdm 手术代码,
ssmc 手术名称,
qkyhdj 切口愈合等级,
ssjb 手术级别,
ssys 手术医生,
ssysmc 手术医生名称,
sqzd 术前诊断,
shzd 术后诊断,
mzfs 麻醉方式,
mzys 麻醉医师,
ssbfz 手术并发症,
sszh 手术组号,
zcbz 主次标志,
ssks 手术科室,
ssksmc 手术科室名称,
ssyzmc 手术一助名称,
ssezmc 手术二助名称,
ssfl 手术分类

from [ICD_RXA].dbo.Fact_ZYBASSK (nolock)




GO
/****** Object:  View [dbo].[11_发血记录]    Script Date: 2025/5/30 9:50:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


---根据数据模型说明创建视图
create view [dbo].[11_发血记录]
as 
select
EMPI	患者主索引,
jzlb	就诊类别,
patid	患者编号,
jzlsh	患者就诊唯一编号,
hzxm	患者姓名,
Applyno	输血编号,
ApplyTime	申请时间,
SendBloodTime	发血时间,
ABOType	病人血型,
ProvideCode	献血码,
ComponentCode	成分码,
ComponentName	成分名称,
Unit	单位,
BloodAmount	发血量,
OrgApplyNo	条码号,
ClinicDesc	临床诊断,
ApplyDept	申请科室代码,
ApplyDeptName	申请科室名称,
SerialNo	发血明细编号



from [ICD_RXA].dbo.Fact_FXJL (nolock)



GO
/****** Object:  View [dbo].[12_住院病历文档]    Script Date: 2025/5/30 9:50:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


---根据数据模型说明创建视图
create view [dbo].[12_住院病历文档]
as 
select
EMPI	患者主索引,
jzlb	就诊类别,
patid	患者编号,
jzlsh	患者就诊唯一编号,
hzxm	患者姓名,
blxh	病历序号,
blmc	病历名称,
wjjgxh	文件结构序号,
wjjgmc	文件结构名称,
wjjgxsmc 文件结构显示名称,
blnr  病历内容,
blzt 	有效状态,
ksdm 	科室代码 ,
ksmc 	科室名称,
bqdm 	病区代码,
bqmc 	病区名称,
ryrq 入院日期,
cyrq 出院日期,
cjsj 创建时间,
shsj 审核时间,
ysdm 	创建医生,
ysmc 	创建医生姓名



from [ICD_RXA].dbo.Fact_ZYBLWD (nolock)



GO
/****** Object:  View [dbo].[13_住院护理体征]    Script Date: 2025/5/30 9:50:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO



---住院护理体征
create view [dbo].[13_住院护理体征]
as 
select
EMPI	患者主索引
,jzlb	就诊类别 
,patid	患者编号 
,jzlsh	患者就诊唯一编号 
,hzxm	患者姓名
,ksmc		科室名称
,bqmc		病区名称
,cwdm		床位代码
,zyrq		住院日期
,cjsj		采集时间
,tzdm		体征代码
,tzmc		体征名称 
,tzvalue_numeric	 	体征测量值
,tzdw	 	体征单位
from [ICD_RXA].dbo.Fact_ZYHLTZ (nolock)
where jlzt=0




GO
/****** Object:  View [dbo].[14_住院病案首页]    Script Date: 2025/5/30 9:50:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

create view [dbo].[14_住院病案首页]
as 
select 
EMPI 患者主索引 , 
jzlb 就诊类别, 
patid  患者编号, 
jzlsh 患者就诊唯一编号,
hzxm 患者姓名,
basyxh 病案首页序号,
bahm 病案号码,
zyhm 住院号码,
brxz 病人性质,
brly 患者来源,
jkkh 健康卡号,
rycs 入院次数,
xb 性别,
csny 出生年月,
xsnl 年龄,
sfzh 身份证号,
lxdh 联系电话,
lxdz 联系地址,
ryrq 入院日期,
cyrq 出院日期,
zkrq 转科日期,
qzrq 确诊日期,
zyts 住院天数,
sqts 术前天数 ,
shts 术后天数,
ryks 入院科室,
ryksmc 入院科室名称,
rybq 入院病区,
rybqmc 入院病区名称,
rych 入院床号,
zkks 转科科室,
zkksmc 转科科室名称,
zkbq 转科病区,
zkbqmc 转科病区名称,
zkch 转科床号,
cyks 出院科室,
cyksmc 出院科室名称,
cybq 出院病区,
cybqmc 出院病区名称,
cych 出院床号,
mzzd 门诊诊断编码,
mzzdmc 门诊诊断名称,
ryzd 入院诊断编码,
ryzdmc 入院诊断名称,
zyzd 主要诊断,
zyzdmc 主要诊断名称,
blzd 病理诊断,
blzdmc 病理诊断名称,
blbh 病理号,
zljg 治疗结果,
mzzd_zy 中医门诊诊断,
mzzd_zymc 中医门诊诊断名称,
zyzd_zy 中医主症编码,
zyzd_zymc 中医主症名称,
zyzz_zy	 中医主症疾病编码,
zyzz_zymc 中医主症疾病名称,
zgqk_zy 中医转归情况,
bfz 并发症,bfjg 并发结果,
yndygr 院内感染,
gmyw 过敏药物,
xx 血型,RH RH,
qjcs 抢救次数,
cgcs 成功次数,
grcs 感染次数,
mcfh 门出符合,
rcfh 入出符合,
qhfh 手术前后符合,
blfh 病理符合,
fsfh 放射符合,
ctfh CT符合,
cyfs 离院方式,
zyys 住院医生,
zyysmc 住院医生姓名,
zzys 主治医生,
zzysmc 主治医生姓名,
zrys 主任医生,
zrysmc 主任医生姓名,
kzr 科主任,
kzrmc 科主任姓名,
zfy 总费用,
wz 是否危重,
yn 是否疑难,
shzt 审核状态,
shrq 审核时间
from [ICD_RXA].dbo.Fact_ZYBASYK (nolock)



GO
/****** Object:  View [dbo].[15_住院病人首页]    Script Date: 2025/5/30 9:50:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

create view [dbo].[15_住院病人首页]
as 
select 
EMPI 患者主索引,
jzlb 就诊类别,
patid 患者编号,
jzlsh 患者就诊唯一编号,
hzxm 患者姓名,
mzh 门诊号,
blh 病历号,
sfzh 身份证号,
sex 性别,
birthday 生日,
jznl 就诊时年龄,
brzt 患者状态,
ryrq 入院日期,
rqrq 入区日期,
cyrq 出院日期,
cqrq 出区日期,
ksdm 科室代码,
ksmc 科室名称,
bqdm 病区代码,
bqmc 病区名称,
ysdm 医生代码,
ysmc 医生名称,
cwdm 床位代码,
CardNo 卡号,
ybdm 医保类型,
lxr 联系人,
lxrdh 联系人电话,
lxrdz 联系人地址,
gmxx 过敏信息
from [ICD_RXA].dbo.Fact_ZYBRSYK (nolock)



GO
/****** Object:  View [dbo].[16_住院转诊]    Script Date: 2025/5/30 9:50:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO



---根据数据模型说明创建视图
create view [dbo].[16_住院转诊]
as 
select
EMPI	患者主索引,
jzlb	就诊类别,
patid	患者编号,
jzlsh	患者就诊唯一编号,
hzxm	患者姓名,
xh	序号,
bqdm 病区代码,
bqmc 病区名称,
ksdm 科室代码,
ksmc 科室名称,
cwdm 床位代码,
zcbqdm 转出病区代码,
zcbqmc 转出病区名称,
zcksdm 转出科室代码,
zcksmc 转出科室名称,
zccwdm 转出床位代码,
ksrq 开始时间,
jsrq 结束时间




from [ICD_RXA].dbo.Fact_ZYZZJH (nolock)




GO
/****** Object:  View [dbo].[17_门诊挂号诊断]    Script Date: 2025/5/30 9:50:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


---根据数据模型说明创建视图
create view [dbo].[17_门诊挂号诊断]
as 
select
EMPI	 患者主索引,
jzlb	 就诊类别,
patid	 患者编号,
jzlsh	 患者就诊唯一编号,
hzxm	 患者姓名,
sfzh	 身份证号,
CardNo   卡号,
ybdm   	 医保类型,
jznl	 就诊时年龄,
jzrq	 就诊日期,
ksdm	 科室代码,
ksmc	 科室名称,
ysdm	 医生代码,
ysmc	 医生名称,
scysdm	 首次接诊医生代码,
scysmc	 首次接诊医生名称,
ghlb	 挂号类别,
ghzt	 挂号状态,
fzzt	 分诊状态






from [ICD_RXA].dbo.Fact_MZGHZDK (nolock)



GO
/****** Object:  View [dbo].[18_门诊诊断]    Script Date: 2025/5/30 9:50:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


---根据数据模型说明创建视图
create view [dbo].[18_门诊诊断]
as 
select
EMPI	患者主索引,
jzlb	就诊类别,
patid	患者编号,
jzlsh	患者就诊唯一编号,
hzxm	患者姓名,
zdrq	诊断日期,
zddm	诊断代码,
zdmc	诊断名称,
zdlx	诊断类型,
zdlb	诊断类别,
zhmc	症候名称,
ysdm	医生代码,
ysmc	医生姓名







from [ICD_RXA].dbo.Fact_MZZD (nolock)



GO
/****** Object:  View [dbo].[19_住院诊断]    Script Date: 2025/5/30 9:50:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE view [dbo].[19_住院诊断]
as 
select
EMPI 患者主索引,
jzlb 就诊类别,
patid 患者编号,
jzlsh 患者就诊唯一编号,
hzxm 患者姓名,
zdlb 诊断类别,
case when zdlx like '%辅诊%'  then case when convert(int ,replace(replace(zdlx ,'辅诊',''),'第',''))>900 then '辅诊' else  zdlx end else  zdlx end  诊断类型,
zddm 诊断代码,
zdmc 诊断名称,
ysdm 医生代码,
ysmc 医生姓名,
zdrq 诊断日期,
zxdm 症型代码,
zxmc 症型名称,
memo 备注
from [ICD_RXA].dbo.Fact_ZYZD (nolock)
where  zdmc <>''




GO
/****** Object:  View [dbo].[20_住院病案诊断]    Script Date: 2025/5/30 9:50:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

create view [dbo].[20_住院病案诊断]
as 
select 
EMPI 患者主索引,
jzlb 就诊类别,
patid 患者编号,
jzlsh 患者就诊唯一编号,
hzxm 患者姓名,
basyxh 病案首页序号,
zdxh 诊断序号,
zddm 诊断代码,
zdmc 诊断名称,
zldm 肿瘤代码,
zlmc 肿瘤名称,
rybq 入院病情,
zgqk 出院情况
from [ICD_RXA].dbo.Fact_ZYBAZDK (nolock)



GO
/****** Object:  View [dbo].[21_脊柱侧弯筛查]    Script Date: 2025/5/30 9:50:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


---根据数据模型说明创建视图
create view [dbo].[21_脊柱侧弯筛查]
as 
select
	ID 系统ID号,
	xxmc 学校名称,
	bjmc 班级名称,
	xm 学生姓名,
	xjh 学籍号,
	xh 学号,
	xb 性别,
	sr 生日,
	sjh 手机号,
	scdd 筛查地点,
	jcrq 检查日期,
	ttpf_j 体态评分_肩,
	ttpf_jjg 体态评分_肩胛骨,
	ttpf_bx 体态评分_半胸,
	ttpf_y 体态评分_腰,
	zf 总分,
	ttxj 体态小结,
	Cobb Cobb角度,
	xzqd 胸椎曲度,
	yzqd 腰椎曲度,
	C7 C7旋转角度,
	T1 T1旋转角度,
	T2 T2旋转角度,
	T3 T3旋转角度,
	T4 T4旋转角度,
	T5 T5旋转角度,
	T6 T6旋转角度,
	T7 T7旋转角度,
	T8 T8旋转角度,
	T9 T9旋转角度,
	T10 T10旋转角度,
	T11 T11旋转角度,
	T12 T12旋转角度,
	L1 L1旋转角度,
	L2 L2旋转角度,
	L3 L3旋转角度,
	L4 L4旋转角度,
	L5 L5旋转角度,
	jljy 结论和建议,
	pic 影像,
	stid 学生ID,
	nj 年级,
	create_time 创建日期,
	sg 身高,
	tz 体重,
	jc 届次,
	schid 学校ID,
	bjid 班级ID



from [ICD_RXA].dbo.Spinal_Info (nolock)



GO
SET IDENTITY_INSERT [dbo].[AllowedTables] ON 

GO
INSERT [dbo].[AllowedTables] ([Id], [UserId], [TableName], [CanRead], [CanWrite], [CanExport]) VALUES (89, N'ab6f8df4-8537-470d-831b-b5755a38de45', N'01_患者信息', 1, 0, 0)
GO
INSERT [dbo].[AllowedTables] ([Id], [UserId], [TableName], [CanRead], [CanWrite], [CanExport]) VALUES (90, N'61b88454-7d96-47a5-8187-02892908172d', N'01_患者信息', 1, 0, 0)
GO
INSERT [dbo].[AllowedTables] ([Id], [UserId], [TableName], [CanRead], [CanWrite], [CanExport]) VALUES (91, N'7d5225f5-b75e-49bf-ba60-b094d19d154a', N'01_患者信息', 1, 0, 0)
GO
INSERT [dbo].[AllowedTables] ([Id], [UserId], [TableName], [CanRead], [CanWrite], [CanExport]) VALUES (92, N'13d35bbe-0138-43c0-9b0f-073b69e97122', N'01_患者信息', 1, 0, 0)
GO
INSERT [dbo].[AllowedTables] ([Id], [UserId], [TableName], [CanRead], [CanWrite], [CanExport]) VALUES (93, N'ab6f8df4-8537-470d-831b-b5755a38de45', N'21_脊柱侧弯筛查', 1, 0, 0)
GO
INSERT [dbo].[AllowedTables] ([Id], [UserId], [TableName], [CanRead], [CanWrite], [CanExport]) VALUES (94, N'61b88454-7d96-47a5-8187-02892908172d', N'21_脊柱侧弯筛查', 1, 0, 0)
GO
INSERT [dbo].[AllowedTables] ([Id], [UserId], [TableName], [CanRead], [CanWrite], [CanExport]) VALUES (95, N'7d5225f5-b75e-49bf-ba60-b094d19d154a', N'21_脊柱侧弯筛查', 1, 0, 0)
GO
INSERT [dbo].[AllowedTables] ([Id], [UserId], [TableName], [CanRead], [CanWrite], [CanExport]) VALUES (96, N'13d35bbe-0138-43c0-9b0f-073b69e97122', N'21_脊柱侧弯筛查', 1, 0, 0)
GO
SET IDENTITY_INSERT [dbo].[AllowedTables] OFF
GO
SET IDENTITY_INSERT [dbo].[ColumnInfos] ON 

GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1893, 53, N'患者主索引', N'患者主索引', N'varchar', 0, 0, N'视图列: 患者主索引, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1894, 53, N'就诊类别', N'就诊类别', N'varchar', 0, 0, N'视图列: 就诊类别, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1895, 53, N'患者编号', N'患者编号', N'numeric', 0, 0, N'视图列: 患者编号, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1896, 53, N'患者姓名', N'患者姓名', N'varchar', 0, 0, N'视图列: 患者姓名, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1897, 53, N'身份证号', N'身份证号', N'varchar', 0, 1, N'视图列: 身份证号, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1898, 53, N'患者性别', N'患者性别', N'varchar', 0, 1, N'视图列: 患者性别, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1899, 53, N'生日', N'生日', N'date', 0, 1, N'视图列: 生日, 类型: date')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1900, 53, N'婚姻状况', N'婚姻状况', N'varchar', 0, 1, N'视图列: 婚姻状况, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1901, 53, N'病历号', N'病历号', N'varchar', 0, 1, N'视图列: 病历号, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1902, 53, N'卡号', N'卡号', N'varchar', 0, 1, N'视图列: 卡号, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1903, 53, N'医保类型', N'医保类型', N'varchar', 0, 1, N'视图列: 医保类型, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1904, 53, N'联系地址', N'联系地址', N'varchar', 0, 1, N'视图列: 联系地址, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1905, 53, N'联系电话', N'联系电话', N'varchar', 0, 1, N'视图列: 联系电话, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1906, 53, N'登记日期', N'登记日期', N'date', 0, 1, N'视图列: 登记日期, 类型: date')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1907, 54, N'患者主索引', N'患者主索引', N'varchar', 0, 0, N'视图列: 患者主索引, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1908, 54, N'就诊类别', N'就诊类别', N'varchar', 0, 1, N'视图列: 就诊类别, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1909, 54, N'患者编号', N'患者编号', N'numeric', 0, 1, N'视图列: 患者编号, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1910, 54, N'患者就诊唯一编号', N'患者就诊唯一编号', N'numeric', 0, 1, N'视图列: 患者就诊唯一编号, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1911, 54, N'患者姓名', N'患者姓名', N'varchar', 0, 1, N'视图列: 患者姓名, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1912, 54, N'申请单', N'申请单', N'int', 0, 0, N'视图列: 申请单, 类型: int')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1913, 54, N'申请时间', N'申请时间', N'datetime', 0, 1, N'视图列: 申请时间, 类型: datetime')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1914, 54, N'检查时间', N'检查时间', N'datetime', 0, 1, N'视图列: 检查时间, 类型: datetime')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1915, 54, N'报告时间', N'报告时间', N'datetime', 0, 1, N'视图列: 报告时间, 类型: datetime')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1916, 54, N'发布时间', N'发布时间', N'datetime', 0, 1, N'视图列: 发布时间, 类型: datetime')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1917, 54, N'项目代码', N'项目代码', N'varchar', 0, 1, N'视图列: 项目代码, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1918, 54, N'项目名称', N'项目名称', N'varchar', 0, 1, N'视图列: 项目名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1919, 54, N'部位', N'部位', N'varchar', 0, 1, N'视图列: 部位, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1920, 54, N'检查所见', N'检查所见', N'varchar', 0, 1, N'视图列: 检查所见, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1921, 54, N'检查结论', N'检查结论', N'varchar', 0, 1, N'视图列: 检查结论, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1922, 54, N'影像号', N'影像号', N'varchar', 0, 1, N'视图列: 影像号, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1923, 54, N'状态', N'状态', N'varchar', 0, 1, N'视图列: 状态, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1924, 54, N'申请科室', N'申请科室', N'varchar', 0, 1, N'视图列: 申请科室, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1925, 54, N'申请科室名称', N'申请科室名称', N'varchar', 0, 1, N'视图列: 申请科室名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1926, 55, N'患者主索引', N'患者主索引', N'varchar', 0, 0, N'视图列: 患者主索引, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1927, 55, N'就诊类别', N'就诊类别', N'varchar', 0, 0, N'视图列: 就诊类别, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1928, 55, N'患者编号', N'患者编号', N'numeric', 0, 0, N'视图列: 患者编号, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1929, 55, N'患者就诊唯一编号', N'患者就诊唯一编号', N'numeric', 0, 1, N'视图列: 患者就诊唯一编号, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1930, 55, N'患者姓名', N'患者姓名', N'varchar', 0, 0, N'视图列: 患者姓名, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1931, 55, N'申请单', N'申请单', N'int', 0, 0, N'视图列: 申请单, 类型: int')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1932, 55, N'样本名称', N'样本名称', N'varchar', 0, 1, N'视图列: 样本名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1933, 55, N'申请时间', N'申请时间', N'datetime', 0, 1, N'视图列: 申请时间, 类型: datetime')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1934, 55, N'采样时间', N'采样时间', N'datetime', 0, 1, N'视图列: 采样时间, 类型: datetime')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1935, 55, N'收到日期', N'收到日期', N'datetime', 0, 1, N'视图列: 收到日期, 类型: datetime')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1936, 55, N'检验日期', N'检验日期', N'datetime', 0, 1, N'视图列: 检验日期, 类型: datetime')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1937, 55, N'报告日期', N'报告日期', N'datetime', 0, 1, N'视图列: 报告日期, 类型: datetime')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1938, 55, N'结果日期', N'结果日期', N'datetime', 0, 1, N'视图列: 结果日期, 类型: datetime')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1939, 55, N'项目代码', N'项目代码', N'varchar', 0, 1, N'视图列: 项目代码, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1940, 55, N'项目名称', N'项目名称', N'varchar', 0, 1, N'视图列: 项目名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1941, 55, N'指标代码', N'指标代码', N'varchar', 0, 1, N'视图列: 指标代码, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1942, 55, N'指标名称', N'指标名称', N'varchar', 0, 1, N'视图列: 指标名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1943, 55, N'结果', N'结果', N'varchar', 0, 1, N'视图列: 结果, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1944, 55, N'标识', N'标识', N'varchar', 0, 1, N'视图列: 标识, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1945, 55, N'参考值', N'参考值', N'varchar', 0, 1, N'视图列: 参考值, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1946, 55, N'单位', N'单位', N'varchar', 0, 1, N'视图列: 单位, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1947, 55, N'状态', N'状态', N'varchar', 0, 1, N'视图列: 状态, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1948, 55, N'申请科室', N'申请科室', N'varchar', 0, 1, N'视图列: 申请科室, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1949, 55, N'申请科室名称', N'申请科室名称', N'varchar', 0, 1, N'视图列: 申请科室名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1950, 55, N'结果表编号', N'结果表编号', N'int', 0, 0, N'视图列: 结果表编号, 类型: int')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1951, 56, N'患者主索引', N'患者主索引', N'varchar', 0, 0, N'视图列: 患者主索引, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1952, 56, N'就诊类别', N'就诊类别', N'varchar', 0, 1, N'视图列: 就诊类别, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1953, 56, N'患者编号', N'患者编号', N'numeric', 0, 1, N'视图列: 患者编号, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1954, 56, N'患者就诊唯一编号', N'患者就诊唯一编号', N'numeric', 0, 1, N'视图列: 患者就诊唯一编号, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1955, 56, N'患者姓名', N'患者姓名', N'varchar', 0, 1, N'视图列: 患者姓名, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1956, 56, N'报告单号', N'报告单号', N'int', 0, 0, N'视图列: 报告单号, 类型: int')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1957, 56, N'年龄', N'年龄', N'varchar', 0, 1, N'视图列: 年龄, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1958, 56, N'申请科室', N'申请科室', N'varchar', 0, 1, N'视图列: 申请科室, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1959, 56, N'申请科室名称', N'申请科室名称', N'varchar', 0, 1, N'视图列: 申请科室名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1960, 56, N'申请时间', N'申请时间', N'datetime', 0, 1, N'视图列: 申请时间, 类型: datetime')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1961, 56, N'接收时间', N'接收时间', N'datetime', 0, 1, N'视图列: 接收时间, 类型: datetime')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1962, 56, N'报告时间', N'报告时间', N'datetime', 0, 1, N'视图列: 报告时间, 类型: datetime')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1963, 56, N'发布时间', N'发布时间', N'datetime', 0, 1, N'视图列: 发布时间, 类型: datetime')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1964, 56, N'病理号', N'病理号', N'varchar', 0, 1, N'视图列: 病理号, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1965, 56, N'临床诊断', N'临床诊断', N'varchar', 0, 1, N'视图列: 临床诊断, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1966, 56, N'病理报告名称', N'病理报告名称', N'varchar', 0, 1, N'视图列: 病理报告名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1967, 56, N'病理诊断', N'病理诊断', N'varchar', 0, 1, N'视图列: 病理诊断, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1968, 56, N'状态', N'状态', N'varchar', 0, 1, N'视图列: 状态, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1969, 56, N'性别', N'性别', N'varchar', 0, 1, N'视图列: 性别, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1970, 57, N'患者主索引', N'患者主索引', N'varchar', 0, 0, N'视图列: 患者主索引, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1971, 57, N'就诊类别', N'就诊类别', N'varchar', 0, 0, N'视图列: 就诊类别, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1972, 57, N'患者编号', N'患者编号', N'numeric', 0, 0, N'视图列: 患者编号, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1973, 57, N'患者就诊唯一编号', N'患者就诊唯一编号', N'numeric', 0, 1, N'视图列: 患者就诊唯一编号, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1974, 57, N'患者姓名', N'患者姓名', N'varchar', 0, 1, N'视图列: 患者姓名, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1975, 57, N'细菌编号', N'细菌编号', N'int', 0, 0, N'视图列: 细菌编号, 类型: int')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1976, 57, N'申请单号', N'申请单号', N'int', 0, 0, N'视图列: 申请单号, 类型: int')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1977, 57, N'鉴定编号', N'鉴定编号', N'int', 0, 0, N'视图列: 鉴定编号, 类型: int')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1978, 57, N'细菌代码', N'细菌代码', N'varchar', 0, 1, N'视图列: 细菌代码, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1979, 57, N'细菌名称', N'细菌名称', N'varchar', 0, 1, N'视图列: 细菌名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1980, 57, N'标本名称', N'标本名称', N'varchar', 0, 1, N'视图列: 标本名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1981, 57, N'鉴定方法', N'鉴定方法', N'varchar', 0, 1, N'视图列: 鉴定方法, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1982, 57, N'申请日期', N'申请日期', N'datetime', 0, 1, N'视图列: 申请日期, 类型: datetime')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1983, 57, N'采样日期', N'采样日期', N'datetime', 0, 1, N'视图列: 采样日期, 类型: datetime')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1984, 57, N'接收日期', N'接收日期', N'datetime', 0, 1, N'视图列: 接收日期, 类型: datetime')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1985, 57, N'发布时间', N'发布时间', N'datetime', 0, 1, N'视图列: 发布时间, 类型: datetime')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1986, 57, N'多重耐药菌标志', N'多重耐药菌标志', N'varchar', 0, 1, N'视图列: 多重耐药菌标志, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1987, 57, N'菌落计数', N'菌落计数', N'varchar', 0, 1, N'视图列: 菌落计数, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1988, 57, N'申请科室代码', N'申请科室代码', N'varchar', 0, 1, N'视图列: 申请科室代码, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1989, 57, N'申请科室名称', N'申请科室名称', N'varchar', 0, 1, N'视图列: 申请科室名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1990, 57, N'状态', N'状态', N'varchar', 0, 1, N'视图列: 状态, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1991, 58, N'患者主索引', N'患者主索引', N'varchar', 0, 0, N'视图列: 患者主索引, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1992, 58, N'就诊类别', N'就诊类别', N'varchar', 0, 0, N'视图列: 就诊类别, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1993, 58, N'患者编号', N'患者编号', N'numeric', 0, 0, N'视图列: 患者编号, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1994, 58, N'患者就诊唯一编号', N'患者就诊唯一编号', N'numeric', 0, 1, N'视图列: 患者就诊唯一编号, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1995, 58, N'患者姓名', N'患者姓名', N'varchar', 0, 1, N'视图列: 患者姓名, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1996, 58, N'细菌编号', N'细菌编号', N'int', 0, 0, N'视图列: 细菌编号, 类型: int')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1997, 58, N'申请单号', N'申请单号', N'int', 0, 0, N'视图列: 申请单号, 类型: int')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1998, 58, N'鉴定编号', N'鉴定编号', N'int', 0, 0, N'视图列: 鉴定编号, 类型: int')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (1999, 58, N'细菌代码', N'细菌代码', N'varchar', 0, 1, N'视图列: 细菌代码, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2000, 58, N'细菌名称', N'细菌名称', N'varchar', 0, 1, N'视图列: 细菌名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2001, 58, N'申请日期', N'申请日期', N'datetime', 0, 1, N'视图列: 申请日期, 类型: datetime')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2002, 58, N'采样日期', N'采样日期', N'datetime', 0, 1, N'视图列: 采样日期, 类型: datetime')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2003, 58, N'接收日期', N'接收日期', N'datetime', 0, 1, N'视图列: 接收日期, 类型: datetime')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2004, 58, N'发布时间', N'发布时间', N'datetime', 0, 1, N'视图列: 发布时间, 类型: datetime')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2005, 58, N'多重耐药菌标志', N'多重耐药菌标志', N'varchar', 0, 1, N'视图列: 多重耐药菌标志, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2006, 58, N'菌落计数', N'菌落计数', N'varchar', 0, 1, N'视图列: 菌落计数, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2007, 58, N'抗生素代码', N'抗生素代码', N'varchar', 0, 1, N'视图列: 抗生素代码, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2008, 58, N'抗生素名称', N'抗生素名称', N'varchar', 0, 1, N'视图列: 抗生素名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2009, 58, N'测定值', N'测定值', N'varchar', 0, 1, N'视图列: 测定值, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2010, 58, N'药敏结果', N'药敏结果', N'varchar', 0, 1, N'视图列: 药敏结果, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2011, 58, N'参考值', N'参考值', N'varchar', 0, 1, N'视图列: 参考值, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2012, 58, N'申请科室代码', N'申请科室代码', N'varchar', 0, 1, N'视图列: 申请科室代码, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2013, 58, N'申请科室名称', N'申请科室名称', N'varchar', 0, 1, N'视图列: 申请科室名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2014, 58, N'状态', N'状态', N'varchar', 0, 1, N'视图列: 状态, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2015, 58, N'药敏编号', N'药敏编号', N'int', 0, 0, N'视图列: 药敏编号, 类型: int')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2016, 59, N'患者主索引', N'患者主索引', N'varchar', 0, 0, N'视图列: 患者主索引, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2017, 59, N'就诊类别', N'就诊类别', N'varchar', 0, 0, N'视图列: 就诊类别, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2018, 59, N'患者编号', N'患者编号', N'numeric', 0, 0, N'视图列: 患者编号, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2019, 59, N'患者就诊唯一编号', N'患者就诊唯一编号', N'numeric', 0, 1, N'视图列: 患者就诊唯一编号, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2020, 59, N'患者姓名', N'患者姓名', N'varchar', 0, 1, N'视图列: 患者姓名, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2021, 59, N'身份证号', N'身份证号', N'varchar', 0, 1, N'视图列: 身份证号, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2022, 59, N'就诊时年龄', N'就诊时年龄', N'int', 0, 1, N'视图列: 就诊时年龄, 类型: int')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2023, 59, N'处方日期', N'处方日期', N'datetime', 0, 1, N'视图列: 处方日期, 类型: datetime')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2024, 59, N'科室代码', N'科室代码', N'varchar', 0, 1, N'视图列: 科室代码, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2025, 59, N'科室名称', N'科室名称', N'varchar', 0, 1, N'视图列: 科室名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2026, 59, N'药品代码', N'药品代码', N'varchar', 0, 1, N'视图列: 药品代码, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2027, 59, N'临床药品代码', N'临床药品代码', N'varchar', 0, 1, N'视图列: 临床药品代码, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2028, 59, N'规格代码', N'规格代码', N'varchar', 0, 1, N'视图列: 规格代码, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2029, 59, N'药品名称', N'药品名称', N'varchar', 0, 1, N'视图列: 药品名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2030, 59, N'药品数量', N'药品数量', N'numeric', 0, 1, N'视图列: 药品数量, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2031, 59, N'药品规格', N'药品规格', N'varchar', 0, 1, N'视图列: 药品规格, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2032, 59, N'单位', N'单位', N'varchar', 0, 1, N'视图列: 单位, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2033, 59, N'药品类别', N'药品类别', N'varchar', 0, 1, N'视图列: 药品类别, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2034, 59, N'收费状态', N'收费状态', N'varchar', 0, 1, N'视图列: 收费状态, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2035, 59, N'处方序号', N'处方序号', N'numeric', 0, 0, N'视图列: 处方序号, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2036, 59, N'处方明细序号', N'处方明细序号', N'numeric', 0, 0, N'视图列: 处方明细序号, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2037, 60, N'患者主索引', N'患者主索引', N'varchar', 0, 0, N'视图列: 患者主索引, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2038, 60, N'就诊类别', N'就诊类别', N'varchar', 0, 0, N'视图列: 就诊类别, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2039, 60, N'患者编号', N'患者编号', N'numeric', 0, 0, N'视图列: 患者编号, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2040, 60, N'患者就诊唯一编号', N'患者就诊唯一编号', N'numeric', 0, 0, N'视图列: 患者就诊唯一编号, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2041, 60, N'患者姓名', N'患者姓名', N'varchar', 0, 0, N'视图列: 患者姓名, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2042, 60, N'医嘱编号', N'医嘱编号', N'numeric', 0, 0, N'视图列: 医嘱编号, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2043, 60, N'分组序号', N'分组序号', N'numeric', 0, 0, N'视图列: 分组序号, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2044, 60, N'科室代码', N'科室代码', N'varchar', 0, 1, N'视图列: 科室代码, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2045, 60, N'科室名称', N'科室名称', N'varchar', 0, 1, N'视图列: 科室名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2046, 60, N'病区代码', N'病区代码', N'varchar', 0, 1, N'视图列: 病区代码, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2047, 60, N'病区名称', N'病区名称', N'varchar', 0, 1, N'视图列: 病区名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2048, 60, N'开始日期', N'开始日期', N'datetime', 0, 1, N'视图列: 开始日期, 类型: datetime')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2049, 60, N'停止日期', N'停止日期', N'datetime', 0, 1, N'视图列: 停止日期, 类型: datetime')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2050, 60, N'审核日期', N'审核日期', N'datetime', 0, 1, N'视图列: 审核日期, 类型: datetime')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2051, 60, N'药品代码', N'药品代码', N'varchar', 0, 1, N'视图列: 药品代码, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2052, 60, N'临床药品代码', N'临床药品代码', N'varchar', 0, 1, N'视图列: 临床药品代码, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2053, 60, N'规格代码', N'规格代码', N'varchar', 0, 1, N'视图列: 规格代码, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2054, 60, N'药品名称', N'药品名称', N'varchar', 0, 1, N'视图列: 药品名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2055, 60, N'药品数量', N'药品数量', N'numeric', 0, 1, N'视图列: 药品数量, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2056, 60, N'药品规格', N'药品规格', N'varchar', 0, 1, N'视图列: 药品规格, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2057, 60, N'执行单位', N'执行单位', N'varchar', 0, 1, N'视图列: 执行单位, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2058, 60, N'药品剂量', N'药品剂量', N'varchar', 0, 1, N'视图列: 药品剂量, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2059, 60, N'剂量单位', N'剂量单位', N'varchar', 0, 1, N'视图列: 剂量单位, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2060, 60, N'药品用法', N'药品用法', N'varchar', 0, 1, N'视图列: 药品用法, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2061, 60, N'药品频次', N'药品频次', N'varchar', 0, 1, N'视图列: 药品频次, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2062, 60, N'医嘱类别', N'医嘱类别', N'varchar', 0, 1, N'视图列: 医嘱类别, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2063, 60, N'医嘱类型', N'医嘱类型', N'varchar', 0, 0, N'视图列: 医嘱类型, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2064, 60, N'医嘱状态', N'医嘱状态', N'varchar', 0, 1, N'视图列: 医嘱状态, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2065, 60, N'医嘱内容', N'医嘱内容', N'varchar', 0, 1, N'视图列: 医嘱内容, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2066, 61, N'患者主索引', N'患者主索引', N'varchar', 0, 0, N'视图列: 患者主索引, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2067, 61, N'就诊类别', N'就诊类别', N'varchar', 0, 0, N'视图列: 就诊类别, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2068, 61, N'患者编号', N'患者编号', N'numeric', 0, 0, N'视图列: 患者编号, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2069, 61, N'患者就诊唯一编号', N'患者就诊唯一编号', N'numeric', 0, 0, N'视图列: 患者就诊唯一编号, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2070, 61, N'患者姓名', N'患者姓名', N'varchar', 0, 0, N'视图列: 患者姓名, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2071, 61, N'医嘱编号', N'医嘱编号', N'numeric', 0, 0, N'视图列: 医嘱编号, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2072, 61, N'分组序号', N'分组序号', N'numeric', 0, 0, N'视图列: 分组序号, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2073, 61, N'科室代码', N'科室代码', N'varchar', 0, 1, N'视图列: 科室代码, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2074, 61, N'科室名称', N'科室名称', N'varchar', 0, 1, N'视图列: 科室名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2075, 61, N'病区代码', N'病区代码', N'varchar', 0, 1, N'视图列: 病区代码, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2076, 61, N'病区名称', N'病区名称', N'varchar', 0, 1, N'视图列: 病区名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2077, 61, N'开始日期', N'开始日期', N'datetime', 0, 1, N'视图列: 开始日期, 类型: datetime')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2078, 61, N'停止日期', N'停止日期', N'datetime', 0, 1, N'视图列: 停止日期, 类型: datetime')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2079, 61, N'审核日期', N'审核日期', N'datetime', 0, 1, N'视图列: 审核日期, 类型: datetime')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2080, 61, N'项目代码', N'项目代码', N'varchar', 0, 1, N'视图列: 项目代码, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2081, 61, N'项目名称', N'项目名称', N'varchar', 0, 1, N'视图列: 项目名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2082, 61, N'项目数量', N'项目数量', N'numeric', 0, 1, N'视图列: 项目数量, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2083, 61, N'项目规格', N'项目规格', N'varchar', 0, 1, N'视图列: 项目规格, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2084, 61, N'执行单位', N'执行单位', N'varchar', 0, 1, N'视图列: 执行单位, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2085, 61, N'项目频次', N'项目频次', N'varchar', 0, 1, N'视图列: 项目频次, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2086, 61, N'医嘱类别', N'医嘱类别', N'varchar', 0, 1, N'视图列: 医嘱类别, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2087, 61, N'医嘱类型', N'医嘱类型', N'varchar', 0, 1, N'视图列: 医嘱类型, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2088, 61, N'医嘱状态', N'医嘱状态', N'varchar', 0, 1, N'视图列: 医嘱状态, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2089, 61, N'医嘱内容', N'医嘱内容', N'varchar', 0, 1, N'视图列: 医嘱内容, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2090, 62, N'患者主索引', N'患者主索引', N'varchar', 0, 0, N'视图列: 患者主索引, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2091, 62, N'就诊类别', N'就诊类别', N'varchar', 0, 0, N'视图列: 就诊类别, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2092, 62, N'患者编号', N'患者编号', N'numeric', 0, 0, N'视图列: 患者编号, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2093, 62, N'患者就诊唯一编号', N'患者就诊唯一编号', N'numeric', 0, 0, N'视图列: 患者就诊唯一编号, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2094, 62, N'患者姓名', N'患者姓名', N'varchar', 0, 0, N'视图列: 患者姓名, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2095, 62, N'性别', N'性别', N'varchar', 0, 1, N'视图列: 性别, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2096, 62, N'病案首页序号', N'病案首页序号', N'numeric', 0, 0, N'视图列: 病案首页序号, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2097, 62, N'手术序号', N'手术序号', N'int', 0, 0, N'视图列: 手术序号, 类型: int')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2098, 62, N'手术日期', N'手术日期', N'date', 0, 1, N'视图列: 手术日期, 类型: date')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2099, 62, N'手术代码', N'手术代码', N'varchar', 0, 1, N'视图列: 手术代码, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2100, 62, N'手术名称', N'手术名称', N'varchar', 0, 1, N'视图列: 手术名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2101, 62, N'切口愈合等级', N'切口愈合等级', N'varchar', 0, 1, N'视图列: 切口愈合等级, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2102, 62, N'手术级别', N'手术级别', N'varchar', 0, 1, N'视图列: 手术级别, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2103, 62, N'手术医生', N'手术医生', N'varchar', 0, 1, N'视图列: 手术医生, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2104, 62, N'手术医生名称', N'手术医生名称', N'varchar', 0, 1, N'视图列: 手术医生名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2105, 62, N'术前诊断', N'术前诊断', N'varchar', 0, 1, N'视图列: 术前诊断, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2106, 62, N'术后诊断', N'术后诊断', N'varchar', 0, 1, N'视图列: 术后诊断, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2107, 62, N'麻醉方式', N'麻醉方式', N'varchar', 0, 1, N'视图列: 麻醉方式, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2108, 62, N'麻醉医师', N'麻醉医师', N'varchar', 0, 1, N'视图列: 麻醉医师, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2109, 62, N'手术并发症', N'手术并发症', N'varchar', 0, 1, N'视图列: 手术并发症, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2110, 62, N'手术组号', N'手术组号', N'int', 0, 1, N'视图列: 手术组号, 类型: int')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2111, 62, N'主次标志', N'主次标志', N'varchar', 0, 1, N'视图列: 主次标志, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2112, 62, N'手术科室', N'手术科室', N'varchar', 0, 1, N'视图列: 手术科室, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2113, 62, N'手术科室名称', N'手术科室名称', N'varchar', 0, 1, N'视图列: 手术科室名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2114, 62, N'手术一助名称', N'手术一助名称', N'varchar', 0, 1, N'视图列: 手术一助名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2115, 62, N'手术二助名称', N'手术二助名称', N'varchar', 0, 1, N'视图列: 手术二助名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2116, 62, N'手术分类', N'手术分类', N'varchar', 0, 1, N'视图列: 手术分类, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2117, 63, N'患者主索引', N'患者主索引', N'varchar', 0, 0, N'视图列: 患者主索引, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2118, 63, N'就诊类别', N'就诊类别', N'varchar', 0, 0, N'视图列: 就诊类别, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2119, 63, N'患者编号', N'患者编号', N'numeric', 0, 0, N'视图列: 患者编号, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2120, 63, N'患者就诊唯一编号', N'患者就诊唯一编号', N'numeric', 0, 1, N'视图列: 患者就诊唯一编号, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2121, 63, N'患者姓名', N'患者姓名', N'varchar', 0, 1, N'视图列: 患者姓名, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2122, 63, N'输血编号', N'输血编号', N'int', 0, 0, N'视图列: 输血编号, 类型: int')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2123, 63, N'申请时间', N'申请时间', N'datetime', 0, 1, N'视图列: 申请时间, 类型: datetime')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2124, 63, N'发血时间', N'发血时间', N'datetime', 0, 1, N'视图列: 发血时间, 类型: datetime')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2125, 63, N'病人血型', N'病人血型', N'varchar', 0, 1, N'视图列: 病人血型, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2126, 63, N'献血码', N'献血码', N'varchar', 0, 1, N'视图列: 献血码, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2127, 63, N'成分码', N'成分码', N'varchar', 0, 1, N'视图列: 成分码, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2128, 63, N'成分名称', N'成分名称', N'varchar', 0, 1, N'视图列: 成分名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2129, 63, N'单位', N'单位', N'varchar', 0, 1, N'视图列: 单位, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2130, 63, N'发血量', N'发血量', N'decimal', 0, 1, N'视图列: 发血量, 类型: decimal')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2131, 63, N'条码号', N'条码号', N'varchar', 0, 1, N'视图列: 条码号, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2132, 63, N'临床诊断', N'临床诊断', N'varchar', 0, 1, N'视图列: 临床诊断, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2133, 63, N'申请科室代码', N'申请科室代码', N'varchar', 0, 1, N'视图列: 申请科室代码, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2134, 63, N'申请科室名称', N'申请科室名称', N'varchar', 0, 1, N'视图列: 申请科室名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2135, 63, N'发血明细编号', N'发血明细编号', N'int', 0, 0, N'视图列: 发血明细编号, 类型: int')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2136, 64, N'患者主索引', N'患者主索引', N'varchar', 0, 0, N'视图列: 患者主索引, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2137, 64, N'就诊类别', N'就诊类别', N'varchar', 0, 0, N'视图列: 就诊类别, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2138, 64, N'患者编号', N'患者编号', N'numeric', 0, 0, N'视图列: 患者编号, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2139, 64, N'患者就诊唯一编号', N'患者就诊唯一编号', N'numeric', 0, 0, N'视图列: 患者就诊唯一编号, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2140, 64, N'患者姓名', N'患者姓名', N'varchar', 0, 0, N'视图列: 患者姓名, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2141, 64, N'病历序号', N'病历序号', N'numeric', 0, 0, N'视图列: 病历序号, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2142, 64, N'病历名称', N'病历名称', N'varchar', 0, 1, N'视图列: 病历名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2143, 64, N'文件结构序号', N'文件结构序号', N'int', 0, 1, N'视图列: 文件结构序号, 类型: int')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2144, 64, N'文件结构名称', N'文件结构名称', N'varchar', 0, 1, N'视图列: 文件结构名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2145, 64, N'文件结构显示名称', N'文件结构显示名称', N'varchar', 0, 1, N'视图列: 文件结构显示名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2146, 64, N'病历内容', N'病历内容', N'varchar', 0, 1, N'视图列: 病历内容, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2147, 64, N'有效状态', N'有效状态', N'varchar', 0, 1, N'视图列: 有效状态, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2148, 64, N'科室代码', N'科室代码', N'varchar', 0, 1, N'视图列: 科室代码, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2149, 64, N'科室名称', N'科室名称', N'varchar', 0, 1, N'视图列: 科室名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2150, 64, N'病区代码', N'病区代码', N'varchar', 0, 1, N'视图列: 病区代码, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2151, 64, N'病区名称', N'病区名称', N'varchar', 0, 1, N'视图列: 病区名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2152, 64, N'入院日期', N'入院日期', N'datetime', 0, 1, N'视图列: 入院日期, 类型: datetime')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2153, 64, N'出院日期', N'出院日期', N'datetime', 0, 1, N'视图列: 出院日期, 类型: datetime')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2154, 64, N'创建时间', N'创建时间', N'datetime', 0, 1, N'视图列: 创建时间, 类型: datetime')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2155, 64, N'审核时间', N'审核时间', N'datetime', 0, 1, N'视图列: 审核时间, 类型: datetime')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2156, 64, N'创建医生', N'创建医生', N'varchar', 0, 1, N'视图列: 创建医生, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2157, 64, N'创建医生姓名', N'创建医生姓名', N'varchar', 0, 1, N'视图列: 创建医生姓名, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2158, 65, N'患者主索引', N'患者主索引', N'varchar', 0, 1, N'视图列: 患者主索引, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2159, 65, N'就诊类别', N'就诊类别', N'varchar', 0, 1, N'视图列: 就诊类别, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2160, 65, N'患者编号', N'患者编号', N'numeric', 0, 1, N'视图列: 患者编号, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2161, 65, N'患者就诊唯一编号', N'患者就诊唯一编号', N'numeric', 0, 1, N'视图列: 患者就诊唯一编号, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2162, 65, N'患者姓名', N'患者姓名', N'varchar', 0, 1, N'视图列: 患者姓名, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2163, 65, N'科室名称', N'科室名称', N'varchar', 0, 1, N'视图列: 科室名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2164, 65, N'病区名称', N'病区名称', N'varchar', 0, 1, N'视图列: 病区名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2165, 65, N'床位代码', N'床位代码', N'varchar', 0, 1, N'视图列: 床位代码, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2166, 65, N'住院日期', N'住院日期', N'datetime', 0, 1, N'视图列: 住院日期, 类型: datetime')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2167, 65, N'采集时间', N'采集时间', N'datetime', 0, 1, N'视图列: 采集时间, 类型: datetime')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2168, 65, N'体征代码', N'体征代码', N'varchar', 0, 1, N'视图列: 体征代码, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2169, 65, N'体征名称', N'体征名称', N'varchar', 0, 1, N'视图列: 体征名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2170, 65, N'体征测量值', N'体征测量值', N'numeric', 0, 1, N'视图列: 体征测量值, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2171, 65, N'体征单位', N'体征单位', N'varchar', 0, 1, N'视图列: 体征单位, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2172, 66, N'患者主索引', N'患者主索引', N'varchar', 0, 0, N'视图列: 患者主索引, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2173, 66, N'就诊类别', N'就诊类别', N'varchar', 0, 0, N'视图列: 就诊类别, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2174, 66, N'患者编号', N'患者编号', N'numeric', 0, 0, N'视图列: 患者编号, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2175, 66, N'患者就诊唯一编号', N'患者就诊唯一编号', N'numeric', 0, 0, N'视图列: 患者就诊唯一编号, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2176, 66, N'患者姓名', N'患者姓名', N'varchar', 0, 0, N'视图列: 患者姓名, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2177, 66, N'病案首页序号', N'病案首页序号', N'numeric', 0, 0, N'视图列: 病案首页序号, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2178, 66, N'病案号码', N'病案号码', N'varchar', 0, 0, N'视图列: 病案号码, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2179, 66, N'住院号码', N'住院号码', N'varchar', 0, 0, N'视图列: 住院号码, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2180, 66, N'病人性质', N'病人性质', N'varchar', 0, 0, N'视图列: 病人性质, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2181, 66, N'患者来源', N'患者来源', N'varchar', 0, 0, N'视图列: 患者来源, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2182, 66, N'健康卡号', N'健康卡号', N'varchar', 0, 0, N'视图列: 健康卡号, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2183, 66, N'入院次数', N'入院次数', N'int', 0, 1, N'视图列: 入院次数, 类型: int')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2184, 66, N'性别', N'性别', N'varchar', 0, 1, N'视图列: 性别, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2185, 66, N'出生年月', N'出生年月', N'date', 0, 1, N'视图列: 出生年月, 类型: date')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2186, 66, N'年龄', N'年龄', N'int', 0, 1, N'视图列: 年龄, 类型: int')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2187, 66, N'身份证号', N'身份证号', N'varchar', 0, 1, N'视图列: 身份证号, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2188, 66, N'联系电话', N'联系电话', N'varchar', 0, 1, N'视图列: 联系电话, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2189, 66, N'联系地址', N'联系地址', N'varchar', 0, 1, N'视图列: 联系地址, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2190, 66, N'入院日期', N'入院日期', N'datetime', 0, 1, N'视图列: 入院日期, 类型: datetime')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2191, 66, N'出院日期', N'出院日期', N'datetime', 0, 1, N'视图列: 出院日期, 类型: datetime')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2192, 66, N'转科日期', N'转科日期', N'datetime', 0, 1, N'视图列: 转科日期, 类型: datetime')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2193, 66, N'确诊日期', N'确诊日期', N'date', 0, 1, N'视图列: 确诊日期, 类型: date')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2194, 66, N'住院天数', N'住院天数', N'int', 0, 1, N'视图列: 住院天数, 类型: int')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2195, 66, N'术前天数', N'术前天数', N'int', 0, 1, N'视图列: 术前天数, 类型: int')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2196, 66, N'术后天数', N'术后天数', N'int', 0, 1, N'视图列: 术后天数, 类型: int')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2197, 66, N'入院科室', N'入院科室', N'varchar', 0, 1, N'视图列: 入院科室, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2198, 66, N'入院科室名称', N'入院科室名称', N'varchar', 0, 1, N'视图列: 入院科室名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2199, 66, N'入院病区', N'入院病区', N'varchar', 0, 1, N'视图列: 入院病区, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2200, 66, N'入院病区名称', N'入院病区名称', N'varchar', 0, 1, N'视图列: 入院病区名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2201, 66, N'入院床号', N'入院床号', N'varchar', 0, 1, N'视图列: 入院床号, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2202, 66, N'转科科室', N'转科科室', N'varchar', 0, 1, N'视图列: 转科科室, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2203, 66, N'转科科室名称', N'转科科室名称', N'varchar', 0, 1, N'视图列: 转科科室名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2204, 66, N'转科病区', N'转科病区', N'varchar', 0, 1, N'视图列: 转科病区, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2205, 66, N'转科病区名称', N'转科病区名称', N'varchar', 0, 1, N'视图列: 转科病区名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2206, 66, N'转科床号', N'转科床号', N'varchar', 0, 1, N'视图列: 转科床号, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2207, 66, N'出院科室', N'出院科室', N'varchar', 0, 1, N'视图列: 出院科室, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2208, 66, N'出院科室名称', N'出院科室名称', N'varchar', 0, 1, N'视图列: 出院科室名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2209, 66, N'出院病区', N'出院病区', N'varchar', 0, 1, N'视图列: 出院病区, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2210, 66, N'出院病区名称', N'出院病区名称', N'varchar', 0, 1, N'视图列: 出院病区名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2211, 66, N'出院床号', N'出院床号', N'varchar', 0, 1, N'视图列: 出院床号, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2212, 66, N'门诊诊断编码', N'门诊诊断编码', N'varchar', 0, 1, N'视图列: 门诊诊断编码, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2213, 66, N'门诊诊断名称', N'门诊诊断名称', N'varchar', 0, 1, N'视图列: 门诊诊断名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2214, 66, N'入院诊断编码', N'入院诊断编码', N'varchar', 0, 1, N'视图列: 入院诊断编码, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2215, 66, N'入院诊断名称', N'入院诊断名称', N'varchar', 0, 1, N'视图列: 入院诊断名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2216, 66, N'主要诊断', N'主要诊断', N'varchar', 0, 1, N'视图列: 主要诊断, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2217, 66, N'主要诊断名称', N'主要诊断名称', N'varchar', 0, 1, N'视图列: 主要诊断名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2218, 66, N'病理诊断', N'病理诊断', N'varchar', 0, 1, N'视图列: 病理诊断, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2219, 66, N'病理诊断名称', N'病理诊断名称', N'varchar', 0, 1, N'视图列: 病理诊断名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2220, 66, N'病理号', N'病理号', N'varchar', 0, 1, N'视图列: 病理号, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2221, 66, N'治疗结果', N'治疗结果', N'varchar', 0, 1, N'视图列: 治疗结果, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2222, 66, N'中医门诊诊断', N'中医门诊诊断', N'varchar', 0, 1, N'视图列: 中医门诊诊断, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2223, 66, N'中医门诊诊断名称', N'中医门诊诊断名称', N'varchar', 0, 1, N'视图列: 中医门诊诊断名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2224, 66, N'中医主症编码', N'中医主症编码', N'varchar', 0, 1, N'视图列: 中医主症编码, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2225, 66, N'中医主症名称', N'中医主症名称', N'varchar', 0, 1, N'视图列: 中医主症名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2226, 66, N'中医主症疾病编码', N'中医主症疾病编码', N'varchar', 0, 1, N'视图列: 中医主症疾病编码, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2227, 66, N'中医主症疾病名称', N'中医主症疾病名称', N'varchar', 0, 1, N'视图列: 中医主症疾病名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2228, 66, N'中医转归情况', N'中医转归情况', N'varchar', 0, 1, N'视图列: 中医转归情况, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2229, 66, N'并发症', N'并发症', N'varchar', 0, 1, N'视图列: 并发症, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2230, 66, N'并发结果', N'并发结果', N'varchar', 0, 1, N'视图列: 并发结果, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2231, 66, N'院内感染', N'院内感染', N'varchar', 0, 1, N'视图列: 院内感染, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2232, 66, N'过敏药物', N'过敏药物', N'varchar', 0, 1, N'视图列: 过敏药物, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2233, 66, N'血型', N'血型', N'varchar', 0, 1, N'视图列: 血型, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2234, 66, N'RH', N'RH', N'varchar', 0, 1, N'视图列: RH, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2235, 66, N'抢救次数', N'抢救次数', N'int', 0, 1, N'视图列: 抢救次数, 类型: int')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2236, 66, N'成功次数', N'成功次数', N'int', 0, 1, N'视图列: 成功次数, 类型: int')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2237, 66, N'感染次数', N'感染次数', N'int', 0, 1, N'视图列: 感染次数, 类型: int')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2238, 66, N'门出符合', N'门出符合', N'varchar', 0, 1, N'视图列: 门出符合, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2239, 66, N'入出符合', N'入出符合', N'varchar', 0, 1, N'视图列: 入出符合, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2240, 66, N'手术前后符合', N'手术前后符合', N'varchar', 0, 1, N'视图列: 手术前后符合, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2241, 66, N'病理符合', N'病理符合', N'varchar', 0, 1, N'视图列: 病理符合, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2242, 66, N'放射符合', N'放射符合', N'varchar', 0, 1, N'视图列: 放射符合, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2243, 66, N'CT符合', N'CT符合', N'varchar', 0, 1, N'视图列: CT符合, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2244, 66, N'离院方式', N'离院方式', N'varchar', 0, 1, N'视图列: 离院方式, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2245, 66, N'住院医生', N'住院医生', N'varchar', 0, 1, N'视图列: 住院医生, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2246, 66, N'住院医生姓名', N'住院医生姓名', N'varchar', 0, 1, N'视图列: 住院医生姓名, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2247, 66, N'主治医生', N'主治医生', N'varchar', 0, 1, N'视图列: 主治医生, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2248, 66, N'主治医生姓名', N'主治医生姓名', N'varchar', 0, 1, N'视图列: 主治医生姓名, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2249, 66, N'主任医生', N'主任医生', N'varchar', 0, 1, N'视图列: 主任医生, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2250, 66, N'主任医生姓名', N'主任医生姓名', N'varchar', 0, 1, N'视图列: 主任医生姓名, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2251, 66, N'科主任', N'科主任', N'varchar', 0, 1, N'视图列: 科主任, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2252, 66, N'科主任姓名', N'科主任姓名', N'varchar', 0, 1, N'视图列: 科主任姓名, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2253, 66, N'总费用', N'总费用', N'numeric', 0, 1, N'视图列: 总费用, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2254, 66, N'是否危重', N'是否危重', N'varchar', 0, 1, N'视图列: 是否危重, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2255, 66, N'是否疑难', N'是否疑难', N'varchar', 0, 1, N'视图列: 是否疑难, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2256, 66, N'审核状态', N'审核状态', N'varchar', 0, 1, N'视图列: 审核状态, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2257, 66, N'审核时间', N'审核时间', N'datetime', 0, 1, N'视图列: 审核时间, 类型: datetime')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2258, 67, N'患者主索引', N'患者主索引', N'varchar', 0, 0, N'视图列: 患者主索引, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2259, 67, N'就诊类别', N'就诊类别', N'varchar', 0, 0, N'视图列: 就诊类别, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2260, 67, N'患者编号', N'患者编号', N'numeric', 0, 0, N'视图列: 患者编号, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2261, 67, N'患者就诊唯一编号', N'患者就诊唯一编号', N'numeric', 0, 0, N'视图列: 患者就诊唯一编号, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2262, 67, N'患者姓名', N'患者姓名', N'varchar', 0, 1, N'视图列: 患者姓名, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2263, 67, N'门诊号', N'门诊号', N'varchar', 0, 1, N'视图列: 门诊号, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2264, 67, N'病历号', N'病历号', N'varchar', 0, 1, N'视图列: 病历号, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2265, 67, N'身份证号', N'身份证号', N'varchar', 0, 1, N'视图列: 身份证号, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2266, 67, N'性别', N'性别', N'varchar', 0, 1, N'视图列: 性别, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2267, 67, N'生日', N'生日', N'date', 0, 1, N'视图列: 生日, 类型: date')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2268, 67, N'就诊时年龄', N'就诊时年龄', N'int', 0, 1, N'视图列: 就诊时年龄, 类型: int')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2269, 67, N'患者状态', N'患者状态', N'varchar', 0, 1, N'视图列: 患者状态, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2270, 67, N'入院日期', N'入院日期', N'datetime', 0, 1, N'视图列: 入院日期, 类型: datetime')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2271, 67, N'入区日期', N'入区日期', N'datetime', 0, 1, N'视图列: 入区日期, 类型: datetime')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2272, 67, N'出院日期', N'出院日期', N'datetime', 0, 1, N'视图列: 出院日期, 类型: datetime')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2273, 67, N'出区日期', N'出区日期', N'datetime', 0, 1, N'视图列: 出区日期, 类型: datetime')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2274, 67, N'科室代码', N'科室代码', N'varchar', 0, 1, N'视图列: 科室代码, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2275, 67, N'科室名称', N'科室名称', N'varchar', 0, 1, N'视图列: 科室名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2276, 67, N'病区代码', N'病区代码', N'varchar', 0, 1, N'视图列: 病区代码, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2277, 67, N'病区名称', N'病区名称', N'varchar', 0, 1, N'视图列: 病区名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2278, 67, N'医生代码', N'医生代码', N'varchar', 0, 1, N'视图列: 医生代码, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2279, 67, N'医生名称', N'医生名称', N'varchar', 0, 1, N'视图列: 医生名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2280, 67, N'床位代码', N'床位代码', N'varchar', 0, 1, N'视图列: 床位代码, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2281, 67, N'卡号', N'卡号', N'varchar', 0, 1, N'视图列: 卡号, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2282, 67, N'医保类型', N'医保类型', N'varchar', 0, 1, N'视图列: 医保类型, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2283, 67, N'联系人', N'联系人', N'varchar', 0, 1, N'视图列: 联系人, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2284, 67, N'联系人电话', N'联系人电话', N'varchar', 0, 1, N'视图列: 联系人电话, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2285, 67, N'联系人地址', N'联系人地址', N'varchar', 0, 1, N'视图列: 联系人地址, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2286, 67, N'过敏信息', N'过敏信息', N'varchar', 0, 1, N'视图列: 过敏信息, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2287, 68, N'患者主索引', N'患者主索引', N'varchar', 0, 0, N'视图列: 患者主索引, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2288, 68, N'就诊类别', N'就诊类别', N'varchar', 0, 0, N'视图列: 就诊类别, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2289, 68, N'患者编号', N'患者编号', N'numeric', 0, 0, N'视图列: 患者编号, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2290, 68, N'患者就诊唯一编号', N'患者就诊唯一编号', N'numeric', 0, 0, N'视图列: 患者就诊唯一编号, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2291, 68, N'患者姓名', N'患者姓名', N'varchar', 0, 0, N'视图列: 患者姓名, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2292, 68, N'序号', N'序号', N'numeric', 0, 0, N'视图列: 序号, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2293, 68, N'病区代码', N'病区代码', N'varchar', 0, 1, N'视图列: 病区代码, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2294, 68, N'病区名称', N'病区名称', N'varchar', 0, 1, N'视图列: 病区名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2295, 68, N'科室代码', N'科室代码', N'varchar', 0, 1, N'视图列: 科室代码, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2296, 68, N'科室名称', N'科室名称', N'varchar', 0, 1, N'视图列: 科室名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2297, 68, N'床位代码', N'床位代码', N'varchar', 0, 1, N'视图列: 床位代码, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2298, 68, N'转出病区代码', N'转出病区代码', N'varchar', 0, 1, N'视图列: 转出病区代码, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2299, 68, N'转出病区名称', N'转出病区名称', N'varchar', 0, 1, N'视图列: 转出病区名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2300, 68, N'转出科室代码', N'转出科室代码', N'varchar', 0, 1, N'视图列: 转出科室代码, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2301, 68, N'转出科室名称', N'转出科室名称', N'varchar', 0, 1, N'视图列: 转出科室名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2302, 68, N'转出床位代码', N'转出床位代码', N'varchar', 0, 1, N'视图列: 转出床位代码, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2303, 68, N'开始时间', N'开始时间', N'datetime', 0, 1, N'视图列: 开始时间, 类型: datetime')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2304, 68, N'结束时间', N'结束时间', N'datetime', 0, 1, N'视图列: 结束时间, 类型: datetime')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2305, 45, N'患者主索引', N'患者主索引', N'varchar', 0, 0, N'视图列: 患者主索引, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2306, 45, N'就诊类别', N'就诊类别', N'varchar', 0, 0, N'视图列: 就诊类别, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2307, 45, N'患者编号', N'患者编号', N'numeric', 0, 0, N'视图列: 患者编号, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2308, 45, N'患者就诊唯一编号', N'患者就诊唯一编号', N'numeric', 0, 0, N'视图列: 患者就诊唯一编号, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2309, 45, N'患者姓名', N'患者姓名', N'varchar', 0, 0, N'视图列: 患者姓名, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2310, 45, N'身份证号', N'身份证号', N'varchar', 0, 0, N'视图列: 身份证号, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2311, 45, N'卡号', N'卡号', N'varchar', 0, 1, N'视图列: 卡号, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2312, 45, N'医保类型', N'医保类型', N'varchar', 0, 1, N'视图列: 医保类型, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2313, 45, N'就诊时年龄', N'就诊时年龄', N'int', 0, 1, N'视图列: 就诊时年龄, 类型: int')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2314, 45, N'就诊日期', N'就诊日期', N'datetime', 0, 1, N'视图列: 就诊日期, 类型: datetime')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2315, 45, N'科室代码', N'科室代码', N'varchar', 0, 1, N'视图列: 科室代码, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2316, 45, N'科室名称', N'科室名称', N'varchar', 0, 1, N'视图列: 科室名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2317, 45, N'医生代码', N'医生代码', N'varchar', 0, 1, N'视图列: 医生代码, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2318, 45, N'医生名称', N'医生名称', N'varchar', 0, 1, N'视图列: 医生名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2319, 45, N'首次接诊医生代码', N'首次接诊医生代码', N'varchar', 0, 1, N'视图列: 首次接诊医生代码, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2320, 45, N'首次接诊医生名称', N'首次接诊医生名称', N'varchar', 0, 1, N'视图列: 首次接诊医生名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2321, 45, N'挂号类别', N'挂号类别', N'varchar', 0, 1, N'视图列: 挂号类别, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2322, 45, N'挂号状态', N'挂号状态', N'varchar', 0, 1, N'视图列: 挂号状态, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2323, 45, N'分诊状态', N'分诊状态', N'varchar', 0, 1, N'视图列: 分诊状态, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2324, 47, N'患者主索引', N'患者主索引', N'varchar', 0, 0, N'视图列: 患者主索引, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2325, 47, N'就诊类别', N'就诊类别', N'varchar', 0, 0, N'视图列: 就诊类别, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2326, 47, N'患者编号', N'患者编号', N'numeric', 0, 1, N'视图列: 患者编号, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2327, 47, N'患者就诊唯一编号', N'患者就诊唯一编号', N'numeric', 0, 1, N'视图列: 患者就诊唯一编号, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2328, 47, N'患者姓名', N'患者姓名', N'varchar', 0, 1, N'视图列: 患者姓名, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2329, 47, N'诊断日期', N'诊断日期', N'datetime', 0, 1, N'视图列: 诊断日期, 类型: datetime')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2330, 47, N'诊断代码', N'诊断代码', N'varchar', 0, 1, N'视图列: 诊断代码, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2331, 47, N'诊断名称', N'诊断名称', N'varchar', 0, 1, N'视图列: 诊断名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2332, 47, N'诊断类型', N'诊断类型', N'varchar', 0, 1, N'视图列: 诊断类型, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2333, 47, N'诊断类别', N'诊断类别', N'varchar', 0, 1, N'视图列: 诊断类别, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2334, 47, N'症候名称', N'症候名称', N'varchar', 0, 1, N'视图列: 症候名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2335, 47, N'医生代码', N'医生代码', N'varchar', 0, 1, N'视图列: 医生代码, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2336, 47, N'医生姓名', N'医生姓名', N'varchar', 0, 1, N'视图列: 医生姓名, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2337, 49, N'患者主索引', N'患者主索引', N'varchar', 0, 0, N'视图列: 患者主索引, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2338, 49, N'就诊类别', N'就诊类别', N'varchar', 0, 0, N'视图列: 就诊类别, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2339, 49, N'患者编号', N'患者编号', N'numeric', 0, 0, N'视图列: 患者编号, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2340, 49, N'患者就诊唯一编号', N'患者就诊唯一编号', N'numeric', 0, 0, N'视图列: 患者就诊唯一编号, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2341, 49, N'患者姓名', N'患者姓名', N'varchar', 0, 0, N'视图列: 患者姓名, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2342, 49, N'诊断类别', N'诊断类别', N'varchar', 0, 1, N'视图列: 诊断类别, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2343, 49, N'诊断类型', N'诊断类型', N'varchar', 0, 1, N'视图列: 诊断类型, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2344, 49, N'诊断代码', N'诊断代码', N'varchar', 0, 1, N'视图列: 诊断代码, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2345, 49, N'诊断名称', N'诊断名称', N'varchar', 0, 1, N'视图列: 诊断名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2346, 49, N'医生代码', N'医生代码', N'varchar', 0, 1, N'视图列: 医生代码, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2347, 49, N'医生姓名', N'医生姓名', N'varchar', 0, 1, N'视图列: 医生姓名, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2348, 49, N'诊断日期', N'诊断日期', N'datetime', 0, 1, N'视图列: 诊断日期, 类型: datetime')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2349, 49, N'症型代码', N'症型代码', N'varchar', 0, 1, N'视图列: 症型代码, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2350, 49, N'症型名称', N'症型名称', N'varchar', 0, 1, N'视图列: 症型名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2351, 49, N'备注', N'备注', N'varchar', 0, 1, N'视图列: 备注, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2352, 51, N'患者主索引', N'患者主索引', N'varchar', 0, 0, N'视图列: 患者主索引, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2353, 51, N'就诊类别', N'就诊类别', N'varchar', 0, 0, N'视图列: 就诊类别, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2354, 51, N'患者编号', N'患者编号', N'numeric', 0, 0, N'视图列: 患者编号, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2355, 51, N'患者就诊唯一编号', N'患者就诊唯一编号', N'numeric', 0, 0, N'视图列: 患者就诊唯一编号, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2356, 51, N'患者姓名', N'患者姓名', N'varchar', 0, 0, N'视图列: 患者姓名, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2357, 51, N'病案首页序号', N'病案首页序号', N'numeric', 0, 0, N'视图列: 病案首页序号, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2358, 51, N'诊断序号', N'诊断序号', N'int', 0, 0, N'视图列: 诊断序号, 类型: int')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2359, 51, N'诊断代码', N'诊断代码', N'varchar', 0, 1, N'视图列: 诊断代码, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2360, 51, N'诊断名称', N'诊断名称', N'varchar', 0, 1, N'视图列: 诊断名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2361, 51, N'肿瘤代码', N'肿瘤代码', N'varchar', 0, 1, N'视图列: 肿瘤代码, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2362, 51, N'肿瘤名称', N'肿瘤名称', N'varchar', 0, 1, N'视图列: 肿瘤名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2363, 51, N'入院病情', N'入院病情', N'varchar', 0, 1, N'视图列: 入院病情, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2364, 51, N'出院情况', N'出院情况', N'varchar', 0, 1, N'视图列: 出院情况, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2365, 69, N'系统ID号', N'系统ID号', N'varchar', 0, 0, N'视图列: 系统ID号, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2366, 69, N'学校名称', N'学校名称', N'varchar', 0, 1, N'视图列: 学校名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2367, 69, N'班级名称', N'班级名称', N'varchar', 0, 1, N'视图列: 班级名称, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2368, 69, N'学生姓名', N'学生姓名', N'varchar', 0, 1, N'视图列: 学生姓名, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2369, 69, N'学籍号', N'学籍号', N'varchar', 0, 1, N'视图列: 学籍号, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2370, 69, N'学号', N'学号', N'varchar', 0, 1, N'视图列: 学号, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2371, 69, N'性别', N'性别', N'varchar', 0, 1, N'视图列: 性别, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2372, 69, N'生日', N'生日', N'date', 0, 1, N'视图列: 生日, 类型: date')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2373, 69, N'手机号', N'手机号', N'varchar', 0, 1, N'视图列: 手机号, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2374, 69, N'筛查地点', N'筛查地点', N'varchar', 0, 1, N'视图列: 筛查地点, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2375, 69, N'检查日期', N'检查日期', N'datetime', 0, 1, N'视图列: 检查日期, 类型: datetime')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2376, 69, N'体态评分_肩', N'体态评分_肩', N'varchar', 0, 1, N'视图列: 体态评分_肩, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2377, 69, N'体态评分_肩胛骨', N'体态评分_肩胛骨', N'varchar', 0, 1, N'视图列: 体态评分_肩胛骨, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2378, 69, N'体态评分_半胸', N'体态评分_半胸', N'varchar', 0, 1, N'视图列: 体态评分_半胸, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2379, 69, N'体态评分_腰', N'体态评分_腰', N'varchar', 0, 1, N'视图列: 体态评分_腰, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2380, 69, N'总分', N'总分', N'varchar', 0, 1, N'视图列: 总分, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2381, 69, N'体态小结', N'体态小结', N'varchar', 0, 1, N'视图列: 体态小结, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2382, 69, N'Cobb角度', N'Cobb角度', N'numeric', 0, 1, N'视图列: Cobb角度, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2383, 69, N'胸椎曲度', N'胸椎曲度', N'numeric', 0, 1, N'视图列: 胸椎曲度, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2384, 69, N'腰椎曲度', N'腰椎曲度', N'numeric', 0, 1, N'视图列: 腰椎曲度, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2385, 69, N'C7旋转角度', N'C7旋转角度', N'numeric', 0, 1, N'视图列: C7旋转角度, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2386, 69, N'T1旋转角度', N'T1旋转角度', N'numeric', 0, 1, N'视图列: T1旋转角度, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2387, 69, N'T2旋转角度', N'T2旋转角度', N'numeric', 0, 1, N'视图列: T2旋转角度, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2388, 69, N'T3旋转角度', N'T3旋转角度', N'numeric', 0, 1, N'视图列: T3旋转角度, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2389, 69, N'T4旋转角度', N'T4旋转角度', N'numeric', 0, 1, N'视图列: T4旋转角度, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2390, 69, N'T5旋转角度', N'T5旋转角度', N'numeric', 0, 1, N'视图列: T5旋转角度, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2391, 69, N'T6旋转角度', N'T6旋转角度', N'numeric', 0, 1, N'视图列: T6旋转角度, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2392, 69, N'T7旋转角度', N'T7旋转角度', N'numeric', 0, 1, N'视图列: T7旋转角度, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2393, 69, N'T8旋转角度', N'T8旋转角度', N'numeric', 0, 1, N'视图列: T8旋转角度, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2394, 69, N'T9旋转角度', N'T9旋转角度', N'numeric', 0, 1, N'视图列: T9旋转角度, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2395, 69, N'T10旋转角度', N'T10旋转角度', N'numeric', 0, 1, N'视图列: T10旋转角度, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2396, 69, N'T11旋转角度', N'T11旋转角度', N'numeric', 0, 1, N'视图列: T11旋转角度, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2397, 69, N'T12旋转角度', N'T12旋转角度', N'numeric', 0, 1, N'视图列: T12旋转角度, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2398, 69, N'L1旋转角度', N'L1旋转角度', N'numeric', 0, 1, N'视图列: L1旋转角度, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2399, 69, N'L2旋转角度', N'L2旋转角度', N'numeric', 0, 1, N'视图列: L2旋转角度, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2400, 69, N'L3旋转角度', N'L3旋转角度', N'numeric', 0, 1, N'视图列: L3旋转角度, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2401, 69, N'L4旋转角度', N'L4旋转角度', N'numeric', 0, 1, N'视图列: L4旋转角度, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2402, 69, N'L5旋转角度', N'L5旋转角度', N'numeric', 0, 1, N'视图列: L5旋转角度, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2403, 69, N'结论和建议', N'结论和建议', N'varchar', 0, 1, N'视图列: 结论和建议, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2404, 69, N'影像', N'影像', N'varchar', 0, 1, N'视图列: 影像, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2405, 69, N'学生ID', N'学生ID', N'varchar', 0, 1, N'视图列: 学生ID, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2406, 69, N'年级', N'年级', N'varchar', 0, 1, N'视图列: 年级, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2407, 69, N'创建日期', N'创建日期', N'datetime', 0, 1, N'视图列: 创建日期, 类型: datetime')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2408, 69, N'身高', N'身高', N'numeric', 0, 1, N'视图列: 身高, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2409, 69, N'体重', N'体重', N'numeric', 0, 1, N'视图列: 体重, 类型: numeric')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2410, 69, N'届次', N'届次', N'varchar', 0, 1, N'视图列: 届次, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2411, 69, N'学校ID', N'学校ID', N'varchar', 0, 1, N'视图列: 学校ID, 类型: varchar')
GO
INSERT [dbo].[ColumnInfos] ([Id], [TableId], [ColumnName], [DisplayName], [DataType], [IsPrimaryKey], [IsNullable], [Description]) VALUES (2412, 69, N'班级ID', N'班级ID', N'varchar', 0, 1, N'视图列: 班级ID, 类型: varchar')
GO
SET IDENTITY_INSERT [dbo].[ColumnInfos] OFF
GO
SET IDENTITY_INSERT [dbo].[QueryShares] ON 

GO
INSERT [dbo].[QueryShares] ([Id], [QueryId], [UserId], [SharedAt], [SharedBy]) VALUES (33, 84, N'61b88454-7d96-47a5-8187-02892908172d', CAST(N'2025-05-29 11:51:35.2430000' AS DateTime2), N'ab6f8df4-8537-470d-831b-b5755a38de45')
GO
INSERT [dbo].[QueryShares] ([Id], [QueryId], [UserId], [SharedAt], [SharedBy]) VALUES (34, 84, N'7d5225f5-b75e-49bf-ba60-b094d19d154a', CAST(N'2025-05-29 11:51:35.2470000' AS DateTime2), N'ab6f8df4-8537-470d-831b-b5755a38de45')
GO
INSERT [dbo].[QueryShares] ([Id], [QueryId], [UserId], [SharedAt], [SharedBy]) VALUES (35, 84, N'13d35bbe-0138-43c0-9b0f-073b69e97122', CAST(N'2025-05-29 11:51:35.2470000' AS DateTime2), N'ab6f8df4-8537-470d-831b-b5755a38de45')
GO
SET IDENTITY_INSERT [dbo].[QueryShares] OFF
GO
SET IDENTITY_INSERT [dbo].[RoleClaims] ON 

GO
INSERT [dbo].[RoleClaims] ([Id], [RoleId], [ClaimType], [ClaimValue]) VALUES (1, N'8dc56b5c-019e-424d-bf61-a00d1b0110f0', N'Permission', N'SystemAdmin')
GO
INSERT [dbo].[RoleClaims] ([Id], [RoleId], [ClaimType], [ClaimValue]) VALUES (33, N'e67e94b0-82f9-405f-9e83-a2ee34d95949', N'Permission', N'SaveQueries')
GO
INSERT [dbo].[RoleClaims] ([Id], [RoleId], [ClaimType], [ClaimValue]) VALUES (34, N'e67e94b0-82f9-405f-9e83-a2ee34d95949', N'Permission', N'ShareQueries')
GO
INSERT [dbo].[RoleClaims] ([Id], [RoleId], [ClaimType], [ClaimValue]) VALUES (81, N'7bd8c5ee-a6a7-4c3c-9eb1-abac074e416b', N'Permission', N'ManageUsers')
GO
INSERT [dbo].[RoleClaims] ([Id], [RoleId], [ClaimType], [ClaimValue]) VALUES (82, N'7bd8c5ee-a6a7-4c3c-9eb1-abac074e416b', N'Permission', N'ViewUsers')
GO
INSERT [dbo].[RoleClaims] ([Id], [RoleId], [ClaimType], [ClaimValue]) VALUES (83, N'7bd8c5ee-a6a7-4c3c-9eb1-abac074e416b', N'Permission', N'ManageTables')
GO
INSERT [dbo].[RoleClaims] ([Id], [RoleId], [ClaimType], [ClaimValue]) VALUES (84, N'7bd8c5ee-a6a7-4c3c-9eb1-abac074e416b', N'Permission', N'SaveQueries')
GO
INSERT [dbo].[RoleClaims] ([Id], [RoleId], [ClaimType], [ClaimValue]) VALUES (85, N'7bd8c5ee-a6a7-4c3c-9eb1-abac074e416b', N'Permission', N'ShareQueries')
GO
INSERT [dbo].[RoleClaims] ([Id], [RoleId], [ClaimType], [ClaimValue]) VALUES (86, N'7bd8c5ee-a6a7-4c3c-9eb1-abac074e416b', N'Permission', N'RunComplexQueries')
GO
INSERT [dbo].[RoleClaims] ([Id], [RoleId], [ClaimType], [ClaimValue]) VALUES (87, N'7bd8c5ee-a6a7-4c3c-9eb1-abac074e416b', N'Permission', N'ExportData')
GO
INSERT [dbo].[RoleClaims] ([Id], [RoleId], [ClaimType], [ClaimValue]) VALUES (88, N'45e75ab0-be82-4b8e-879f-3549641548be', N'Permission', N'SaveQueries')
GO
INSERT [dbo].[RoleClaims] ([Id], [RoleId], [ClaimType], [ClaimValue]) VALUES (89, N'45e75ab0-be82-4b8e-879f-3549641548be', N'Permission', N'ExportData')
GO
SET IDENTITY_INSERT [dbo].[RoleClaims] OFF
GO
INSERT [dbo].[Roles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp], [Description]) VALUES (N'45e75ab0-be82-4b8e-879f-3549641548be', N'查询', N'查询', N'44ac9546-321a-40bb-9b54-5a828a9d6829', N'查询')
GO
INSERT [dbo].[Roles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp], [Description]) VALUES (N'7bd8c5ee-a6a7-4c3c-9eb1-abac074e416b', N'一级管理员', N'一级管理员', N'e57da10e-7fbe-4e6e-91c9-a5e55c261d77', N'一级管理员')
GO
INSERT [dbo].[Roles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp], [Description]) VALUES (N'8dc56b5c-019e-424d-bf61-a00d1b0110f0', N'Administrator', N'ADMINISTRATOR', N'd1027c8e-2fff-482a-9283-c4682109f57e', N'系统管理员，拥有所有权限')
GO
INSERT [dbo].[Roles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp], [Description]) VALUES (N'e67e94b0-82f9-405f-9e83-a2ee34d95949', N'分享', N'分享', N'0b3ad3e9-5c66-4829-a47e-c43e2fece655', N'分享')
GO
SET IDENTITY_INSERT [dbo].[SavedQueries] ON 

GO
INSERT [dbo].[SavedQueries] ([Id], [UserId], [Name], [Description], [SqlQuery], [TablesIncluded], [ColumnsIncluded], [FilterConditions], [SortOrder], [CreatedAt], [UpdatedAt], [CreatedBy], [IsShared], [JoinConditions], [IsCustomSql]) VALUES (82, N'ab6f8df4-8537-470d-831b-b5755a38de45', N'单表', N'', N'SELECT
    [01_患者信息].[登记日期] AS [登记日期],
    [01_患者信息].[患者编号] AS [患者编号],
    [01_患者信息].[患者姓名] AS [患者姓名],
    [01_患者信息].[患者性别] AS [患者性别],
    [01_患者信息].[患者主索引] AS [患者主索引],
    [01_患者信息].[婚姻状况] AS [婚姻状况]
FROM
    [01_患者信息]
WHERE
    [01_患者信息].[登记日期] > N''2023-1-1''
    AND [01_患者信息].[登记日期] < N''2024-1-1''
ORDER BY
        [01_患者信息].[患者编号] ASC', N'["01_\u60A3\u8005\u4FE1\u606F"]', N'["01_\u60A3\u8005\u4FE1\u606F.\u767B\u8BB0\u65E5\u671F","01_\u60A3\u8005\u4FE1\u606F.\u60A3\u8005\u7F16\u53F7","01_\u60A3\u8005\u4FE1\u606F.\u60A3\u8005\u59D3\u540D","01_\u60A3\u8005\u4FE1\u606F.\u60A3\u8005\u6027\u522B","01_\u60A3\u8005\u4FE1\u606F.\u60A3\u8005\u4E3B\u7D22\u5F15","01_\u60A3\u8005\u4FE1\u606F.\u5A5A\u59FB\u72B6\u51B5"]', N'["{\u0022column\u0022:\u002201_\u60A3\u8005\u4FE1\u606F.\u767B\u8BB0\u65E5\u671F\u0022,\u0022operator\u0022:\u0022\u003E\u0022,\u0022value\u0022:\u00222023-1-1\u0022,\u0022connector\u0022:\u0022AND\u0022,\u0022sql\u0022:\u0022[01_\u60A3\u8005\u4FE1\u606F].[\u767B\u8BB0\u65E5\u671F] \u003E N\u00272023-1-1\u0027\u0022,\u0022display\u0022:\u0022\u767B\u8BB0\u65E5\u671F \u003E 2023-1-1\u0022}","{\u0022column\u0022:\u002201_\u60A3\u8005\u4FE1\u606F.\u767B\u8BB0\u65E5\u671F\u0022,\u0022operator\u0022:\u0022\u003C\u0022,\u0022value\u0022:\u00222024-1-1\u0022,\u0022connector\u0022:\u0022AND\u0022,\u0022sql\u0022:\u0022[01_\u60A3\u8005\u4FE1\u606F].[\u767B\u8BB0\u65E5\u671F] \u003C N\u00272024-1-1\u0027\u0022,\u0022display\u0022:\u0022\u767B\u8BB0\u65E5\u671F \u003C 2024-1-1\u0022}"]', N'["01_\u60A3\u8005\u4FE1\u606F.\u60A3\u8005\u7F16\u53F7 ASC"]', CAST(N'2025-05-29 11:46:04.5709745' AS DateTime2), CAST(N'2025-05-29 11:46:04.5709752' AS DateTime2), N'admin', 0, N'[]', 0)
GO
INSERT [dbo].[SavedQueries] ([Id], [UserId], [Name], [Description], [SqlQuery], [TablesIncluded], [ColumnsIncluded], [FilterConditions], [SortOrder], [CreatedAt], [UpdatedAt], [CreatedBy], [IsShared], [JoinConditions], [IsCustomSql]) VALUES (84, N'ab6f8df4-8537-470d-831b-b5755a38de45', N'门诊诊断+住院诊断', N'测试', N'SELECT
    [18_门诊诊断].[患者姓名] AS [18_门诊诊断_患者姓名],
    [18_门诊诊断].[患者主索引] AS [18_门诊诊断_患者主索引],
    [18_门诊诊断].[就诊类别] AS [18_门诊诊断_就诊类别],
    [18_门诊诊断].[诊断代码] AS [18_门诊诊断_诊断代码],
    [18_门诊诊断].[诊断类别] AS [18_门诊诊断_诊断类别],
    [18_门诊诊断].[诊断类型] AS [18_门诊诊断_诊断类型],
    [18_门诊诊断].[诊断名称] AS [18_门诊诊断_诊断名称],
    [18_门诊诊断].[诊断日期] AS [18_门诊诊断_诊断日期],
    [19_住院诊断].[患者姓名] AS [19_住院诊断_患者姓名],
    [19_住院诊断].[患者主索引] AS [19_住院诊断_患者主索引],
    [19_住院诊断].[诊断代码] AS [19_住院诊断_诊断代码],
    [19_住院诊断].[诊断类别] AS [19_住院诊断_诊断类别],
    [19_住院诊断].[诊断类型] AS [19_住院诊断_诊断类型],
    [19_住院诊断].[诊断名称] AS [19_住院诊断_诊断名称],
    [19_住院诊断].[诊断日期] AS [19_住院诊断_诊断日期]
FROM
    [18_门诊诊断]
INNER JOIN [19_住院诊断] ON [18_门诊诊断].[患者主索引] = [19_住院诊断].[患者主索引]
WHERE
    [18_门诊诊断].[诊断日期] >= N''2024-1-1''
    AND [18_门诊诊断].[诊断日期] <= N''2024-10-1''
ORDER BY
        [18_门诊诊断].[诊断日期] ASC', N'["18_\u95E8\u8BCA\u8BCA\u65AD","19_\u4F4F\u9662\u8BCA\u65AD"]', N'["18_\u95E8\u8BCA\u8BCA\u65AD.\u60A3\u8005\u59D3\u540D","18_\u95E8\u8BCA\u8BCA\u65AD.\u60A3\u8005\u4E3B\u7D22\u5F15","18_\u95E8\u8BCA\u8BCA\u65AD.\u5C31\u8BCA\u7C7B\u522B","18_\u95E8\u8BCA\u8BCA\u65AD.\u8BCA\u65AD\u4EE3\u7801","18_\u95E8\u8BCA\u8BCA\u65AD.\u8BCA\u65AD\u7C7B\u522B","18_\u95E8\u8BCA\u8BCA\u65AD.\u8BCA\u65AD\u7C7B\u578B","18_\u95E8\u8BCA\u8BCA\u65AD.\u8BCA\u65AD\u540D\u79F0","18_\u95E8\u8BCA\u8BCA\u65AD.\u8BCA\u65AD\u65E5\u671F","19_\u4F4F\u9662\u8BCA\u65AD.\u60A3\u8005\u59D3\u540D","19_\u4F4F\u9662\u8BCA\u65AD.\u60A3\u8005\u4E3B\u7D22\u5F15","19_\u4F4F\u9662\u8BCA\u65AD.\u8BCA\u65AD\u4EE3\u7801","19_\u4F4F\u9662\u8BCA\u65AD.\u8BCA\u65AD\u7C7B\u522B","19_\u4F4F\u9662\u8BCA\u65AD.\u8BCA\u65AD\u7C7B\u578B","19_\u4F4F\u9662\u8BCA\u65AD.\u8BCA\u65AD\u540D\u79F0","19_\u4F4F\u9662\u8BCA\u65AD.\u8BCA\u65AD\u65E5\u671F"]', N'["{\u0022column\u0022:\u002218_\u95E8\u8BCA\u8BCA\u65AD.\u8BCA\u65AD\u65E5\u671F\u0022,\u0022operator\u0022:\u0022\u003E=\u0022,\u0022value\u0022:\u00222024-1-1\u0022,\u0022connector\u0022:\u0022AND\u0022,\u0022sql\u0022:\u0022[18_\u95E8\u8BCA\u8BCA\u65AD].[\u8BCA\u65AD\u65E5\u671F] \u003E= N\u00272024-1-1\u0027\u0022,\u0022display\u0022:\u0022\u8BCA\u65AD\u65E5\u671F \u003E= 2024-1-1\u0022}","{\u0022column\u0022:\u002218_\u95E8\u8BCA\u8BCA\u65AD.\u8BCA\u65AD\u65E5\u671F\u0022,\u0022operator\u0022:\u0022\u003C=\u0022,\u0022value\u0022:\u00222024-10-1\u0022,\u0022connector\u0022:\u0022AND\u0022,\u0022sql\u0022:\u0022[18_\u95E8\u8BCA\u8BCA\u65AD].[\u8BCA\u65AD\u65E5\u671F] \u003C= N\u00272024-10-1\u0027\u0022,\u0022display\u0022:\u0022\u8BCA\u65AD\u65E5\u671F \u003C= 2024-10-1\u0022}"]', N'["18_\u95E8\u8BCA\u8BCA\u65AD.\u8BCA\u65AD\u65E5\u671F ASC"]', CAST(N'2025-05-29 11:49:42.8584827' AS DateTime2), CAST(N'2025-05-29 11:51:35.2330000' AS DateTime2), N'admin', 1, N'["INNER JOIN 19_\u4F4F\u9662\u8BCA\u65AD ON 18_\u95E8\u8BCA\u8BCA\u65AD.\u60A3\u8005\u4E3B\u7D22\u5F15 = 19_\u4F4F\u9662\u8BCA\u65AD.\u60A3\u8005\u4E3B\u7D22\u5F15"]', 0)
GO
SET IDENTITY_INSERT [dbo].[SavedQueries] OFF
GO
SET IDENTITY_INSERT [dbo].[TableInfos] ON 

GO
INSERT [dbo].[TableInfos] ([Id], [TableName], [DisplayName], [Description], [IsView]) VALUES (45, N'17_门诊挂号诊断', N'17_门诊挂号诊断', N'预定义视图: 17_门诊挂号诊断', 1)
GO
INSERT [dbo].[TableInfos] ([Id], [TableName], [DisplayName], [Description], [IsView]) VALUES (47, N'18_门诊诊断', N'18_门诊诊断', N'预定义视图: 18_门诊诊断', 1)
GO
INSERT [dbo].[TableInfos] ([Id], [TableName], [DisplayName], [Description], [IsView]) VALUES (49, N'19_住院诊断', N'19_住院诊断', N'预定义视图: 19_住院诊断', 1)
GO
INSERT [dbo].[TableInfos] ([Id], [TableName], [DisplayName], [Description], [IsView]) VALUES (51, N'20_住院病案诊断', N'20_住院病案诊断', N'预定义视图: 20_住院病案诊断', 1)
GO
INSERT [dbo].[TableInfos] ([Id], [TableName], [DisplayName], [Description], [IsView]) VALUES (53, N'01_患者信息', N'01_患者信息', N'预定义视图: 01_患者信息', 1)
GO
INSERT [dbo].[TableInfos] ([Id], [TableName], [DisplayName], [Description], [IsView]) VALUES (54, N'02_检查结果', N'02_检查结果', N'预定义视图: 02_检查结果', 1)
GO
INSERT [dbo].[TableInfos] ([Id], [TableName], [DisplayName], [Description], [IsView]) VALUES (55, N'03_检验结果', N'03_检验结果', N'预定义视图: 03_检验结果', 1)
GO
INSERT [dbo].[TableInfos] ([Id], [TableName], [DisplayName], [Description], [IsView]) VALUES (56, N'04_病理检查结果', N'04_病理检查结果', N'预定义视图: 04_病理检查结果', 1)
GO
INSERT [dbo].[TableInfos] ([Id], [TableName], [DisplayName], [Description], [IsView]) VALUES (57, N'05_细菌鉴定结果', N'05_细菌鉴定结果', N'预定义视图: 05_细菌鉴定结果', 1)
GO
INSERT [dbo].[TableInfos] ([Id], [TableName], [DisplayName], [Description], [IsView]) VALUES (58, N'06_药敏结果', N'06_药敏结果', N'预定义视图: 06_药敏结果', 1)
GO
INSERT [dbo].[TableInfos] ([Id], [TableName], [DisplayName], [Description], [IsView]) VALUES (59, N'07_门诊处方明细', N'07_门诊处方明细', N'预定义视图: 07_门诊处方明细', 1)
GO
INSERT [dbo].[TableInfos] ([Id], [TableName], [DisplayName], [Description], [IsView]) VALUES (60, N'08_住院药品医嘱', N'08_住院药品医嘱', N'预定义视图: 08_住院药品医嘱', 1)
GO
INSERT [dbo].[TableInfos] ([Id], [TableName], [DisplayName], [Description], [IsView]) VALUES (61, N'09_住院医嘱', N'09_住院医嘱', N'预定义视图: 09_住院医嘱', 1)
GO
INSERT [dbo].[TableInfos] ([Id], [TableName], [DisplayName], [Description], [IsView]) VALUES (62, N'10_住院病案手术', N'10_住院病案手术', N'预定义视图: 10_住院病案手术', 1)
GO
INSERT [dbo].[TableInfos] ([Id], [TableName], [DisplayName], [Description], [IsView]) VALUES (63, N'11_发血记录', N'11_发血记录', N'预定义视图: 11_发血记录', 1)
GO
INSERT [dbo].[TableInfos] ([Id], [TableName], [DisplayName], [Description], [IsView]) VALUES (64, N'12_住院病历文档', N'12_住院病历文档', N'预定义视图: 12_住院病历文档', 1)
GO
INSERT [dbo].[TableInfos] ([Id], [TableName], [DisplayName], [Description], [IsView]) VALUES (65, N'13_住院护理体征', N'13_住院护理体征', N'预定义视图: 13_住院护理体征', 1)
GO
INSERT [dbo].[TableInfos] ([Id], [TableName], [DisplayName], [Description], [IsView]) VALUES (66, N'14_住院病案首页', N'14_住院病案首页', N'预定义视图: 14_住院病案首页', 1)
GO
INSERT [dbo].[TableInfos] ([Id], [TableName], [DisplayName], [Description], [IsView]) VALUES (67, N'15_住院病人首页', N'15_住院病人首页', N'预定义视图: 15_住院病人首页', 1)
GO
INSERT [dbo].[TableInfos] ([Id], [TableName], [DisplayName], [Description], [IsView]) VALUES (68, N'16_住院转诊', N'16_住院转诊', N'预定义视图: 16_住院转诊', 1)
GO
INSERT [dbo].[TableInfos] ([Id], [TableName], [DisplayName], [Description], [IsView]) VALUES (69, N'21_脊柱侧弯筛查', N'21_脊柱侧弯筛查', N'预定义视图: 21_脊柱侧弯筛查', 1)
GO
SET IDENTITY_INSERT [dbo].[TableInfos] OFF
GO
INSERT [dbo].[UserRoles] ([UserId], [RoleId]) VALUES (N'61b88454-7d96-47a5-8187-02892908172d', N'45e75ab0-be82-4b8e-879f-3549641548be')
GO
INSERT [dbo].[UserRoles] ([UserId], [RoleId]) VALUES (N'7d5225f5-b75e-49bf-ba60-b094d19d154a', N'45e75ab0-be82-4b8e-879f-3549641548be')
GO
INSERT [dbo].[UserRoles] ([UserId], [RoleId]) VALUES (N'13d35bbe-0138-43c0-9b0f-073b69e97122', N'7bd8c5ee-a6a7-4c3c-9eb1-abac074e416b')
GO
INSERT [dbo].[UserRoles] ([UserId], [RoleId]) VALUES (N'ab6f8df4-8537-470d-831b-b5755a38de45', N'8dc56b5c-019e-424d-bf61-a00d1b0110f0')
GO
INSERT [dbo].[UserRoles] ([UserId], [RoleId]) VALUES (N'7d5225f5-b75e-49bf-ba60-b094d19d154a', N'e67e94b0-82f9-405f-9e83-a2ee34d95949')
GO
INSERT [dbo].[Users] ([Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [DisplayName], [Department], [CreatedAt], [LastLogin], [IsActive]) VALUES (N'13d35bbe-0138-43c0-9b0f-073b69e97122', N'zr', N'ZR', N'3313@1111.com', N'3313@1111.COM', 1, N'3a1oGiR7ecEOl25Q2eyu7MekDuzki2R7c4gLUwYTu3glLQjk', N'82f2c057-91ee-49b0-8a2c-3b41e372e705', N'83a4c98f-b3b2-449f-b68a-57fd7d8d4b63', N'12342341', 1, 0, NULL, 0, 0, N'主任', N'骨科', CAST(N'2025-05-22 14:58:20.6143121' AS DateTime2), CAST(N'2025-05-29 11:51:10.8058301' AS DateTime2), 1)
GO
INSERT [dbo].[Users] ([Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [DisplayName], [Department], [CreatedAt], [LastLogin], [IsActive]) VALUES (N'61b88454-7d96-47a5-8187-02892908172d', N'cx', N'CX', N'33@11.com', N'33@11.COM', 1, N'eVy3Irs/kz18i6DPK/PPFMCZ5IK6vCNJaZedjv911n4Btsuv', N'08b29b79-43dc-4c9e-ab49-cbe65f84c8fd', N'8d1778a9-47ef-4cb6-8471-82c7d000445a', N'1235234', 1, 0, NULL, 0, 0, N'曹医生', N'骨科', CAST(N'2025-05-07 10:31:43.1878383' AS DateTime2), CAST(N'2025-05-29 11:51:42.3121827' AS DateTime2), 1)
GO
INSERT [dbo].[Users] ([Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [DisplayName], [Department], [CreatedAt], [LastLogin], [IsActive]) VALUES (N'7d5225f5-b75e-49bf-ba60-b094d19d154a', N'fx', N'FX', N'3313@111.com', N'3313@111.COM', 1, N'whbEQAjPCQ4AqmyaPWv+f7XyUFaZpXs00Sz2c3/GnJ7goUO3', N'da1914b8-40c5-457f-b92d-9f637b7d2c5d', N'396435a4-c024-4b25-89fe-3b01cd6c6c1e', N'22222', 1, 0, NULL, 0, 0, N'方医生', N'骨科', CAST(N'2025-05-07 10:45:29.5799888' AS DateTime2), CAST(N'2025-05-20 09:26:01.2033107' AS DateTime2), 1)
GO
INSERT [dbo].[Users] ([Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [DisplayName], [Department], [CreatedAt], [LastLogin], [IsActive]) VALUES (N'ab6f8df4-8537-470d-831b-b5755a38de45', N'admin', N'ADMIN', N'admin@example.com', N'ADMIN@EXAMPLE.COM', 1, N'ARYEPnXlZJlUm5bCHsxaBOJ4b3eQQFI22KCPCXR5vO8UnlIQ', N'9a7c5e52-537c-4928-a189-81aaf74e3459', N'49898316-e489-461d-9558-c2cb729b313b', N'13800138000', 0, 0, NULL, 0, 0, N'系统管理员', N'IT部门', CAST(N'2025-05-06 14:32:41.8235535' AS DateTime2), CAST(N'2025-05-29 16:44:01.1009795' AS DateTime2), 1)
GO
ALTER TABLE [dbo].[AllowedTables] ADD  DEFAULT ((0)) FOR [CanExport]
GO
ALTER TABLE [dbo].[SavedQueries] ADD  DEFAULT ((0)) FOR [IsShared]
GO
ALTER TABLE [dbo].[SavedQueries] ADD  DEFAULT ((0)) FOR [IsCustomSql]
GO
ALTER TABLE [dbo].[TableInfos] ADD  DEFAULT ((0)) FOR [IsView]
GO
ALTER TABLE [dbo].[ColumnInfos]  WITH CHECK ADD  CONSTRAINT [FK_ColumnInfos_TableInfos] FOREIGN KEY([TableId])
REFERENCES [dbo].[TableInfos] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[ColumnInfos] CHECK CONSTRAINT [FK_ColumnInfos_TableInfos]
GO
ALTER TABLE [dbo].[QueryShares]  WITH CHECK ADD  CONSTRAINT [FK_QueryShares_SavedQueries] FOREIGN KEY([QueryId])
REFERENCES [dbo].[SavedQueries] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[QueryShares] CHECK CONSTRAINT [FK_QueryShares_SavedQueries]
GO
ALTER TABLE [dbo].[RoleClaims]  WITH CHECK ADD  CONSTRAINT [FK_RoleClaims_Roles_RoleId] FOREIGN KEY([RoleId])
REFERENCES [dbo].[Roles] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[RoleClaims] CHECK CONSTRAINT [FK_RoleClaims_Roles_RoleId]
GO
ALTER TABLE [dbo].[UserRoles]  WITH CHECK ADD  CONSTRAINT [FK_UserRoles_Roles_RoleId] FOREIGN KEY([RoleId])
REFERENCES [dbo].[Roles] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[UserRoles] CHECK CONSTRAINT [FK_UserRoles_Roles_RoleId]
GO
ALTER TABLE [dbo].[UserRoles]  WITH CHECK ADD  CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[Users] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[UserRoles] CHECK CONSTRAINT [FK_UserRoles_Users_UserId]
GO
