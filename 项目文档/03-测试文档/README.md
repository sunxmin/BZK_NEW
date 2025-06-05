# ? BZK��ѯϵͳ - �����ĵ�ʹ��ָ��

**�汾**: v1.0  
**��������**: 2025��1��3��  
**������**: 2025��1��3��  

## ? �ĵ�����

��Ŀ¼����BZK��ѯϵͳ�����������׼����������ܲ��ԡ����ܲ��ԡ���ȫ���Ե�ȫ��λ�Ĳ��Խ��������

## ? �ļ��ṹ

```
03-�����ĵ�/
������ README.md                          # ���ļ� - ʹ��ָ��
������ Apache_JMeter_��װָ��.md           # JMeter��ϸ��װָ��
������ ��װJMeter.ps1                     # JMeter�Զ���װ�ű�
������ JMeter���ܲ��Խű�����ָ��.md        # JMeter���ú�ʹ��˵��
������ BZK���ܲ���.jmx                    # JMeter���Խű�
������ users.csv                          # �����û�����
������ �����Զ������Դ���.md                # �Զ������Դ���Ϳ��
������ ��ϸ���������ĵ�.md                 # �����Ĳ�����������
������ ��ȫ���Լ���嵥.md                 # ��ȫ�����嵥�ͷ���
������ RunTests.ps1                       # ����ִ�нű�
```

## ? ���ٿ�ʼ

### 1. ����׼��

#### ���蹤��
```bash
# .NET SDK 8.0+
dotnet --version

# Java 8+ (JMeter��Ҫ)
java -version
```

#### Apache JMeter��װ (���ܲ��Ա���)

**��ʽһ��һ���Զ���װ���Ƽ���**
```powershell
# �Թ���Ա�������PowerShell
.\��װJMeter.ps1

# �Զ��尲װ·��
.\��װJMeter.ps1 -InstallPath "E:\tools"

# ���������Ľ���
.\��װJMeter.ps1 -SetChineseLanguage:$false
```

**��ʽ�����ֶ���װ**
1. ���� [Apache JMeter����](https://jmeter.apache.org/download_jmeter.cgi)
2. ���� `apache-jmeter-5.6.3.zip` (Windows) �� `apache-jmeter-5.6.3.tgz` (Linux/macOS)
3. ��ѹ������Ŀ¼���磺`D:\tools\apache-jmeter-5.6.3`��
4. ���û���������
   - `JMETER_HOME` = `D:\tools\apache-jmeter-5.6.3`
   - ��� `%JMETER_HOME%\bin` �� `PATH`
5. ��ѡ���������Ľ��棨�޸� `bin\jmeter.properties` �е� `language=zh_CN`��

**��ϸ��װָ��**: ? [Apache_JMeter_��װָ��.md](Apache_JMeter_��װָ��.md)

#### ��ѡ����
```bash
# Chrome����� (UI������Ҫ)
# OWASP ZAP (��ȫ������Ҫ)
# Visual Studio 2022 (�����͵���)
```

### 2. ��֤��װ

```powershell
# ��֤JMeter��װ
jmeter -version

# ���Խű���֤
.\RunTests.ps1 -TestType "Unit"
```

### 3. ִ�в���

#### ��ʽһ��ʹ��PowerShell�ű����Ƽ���
```powershell
# ִ�����в���
.\RunTests.ps1

# ִ���ض����͵Ĳ���
.\RunTests.ps1 -TestType JMeter
.\RunTests.ps1 -TestType Unit
.\RunTests.ps1 -TestType Integration
.\RunTests.ps1 -TestType UI
.\RunTests.ps1 -TestType Security

# ָ����ͬ�Ļ���URL
.\RunTests.ps1 -BaseUrl "https://test.example.com"

# ��ϸ���ģʽ
.\RunTests.ps1 -VerboseOutput

# ������������
.\RunTests.ps1 -GenerateReport:$false
```

#### ��ʽ�����ֶ�ִ�и������

**JMeter���ܲ���**:
```bash
jmeter -n -t BZK���ܲ���.jmx -l results.jtl -e -o html-report
```

**��Ԫ����**:
```bash
dotnet test --filter "Category=Unit" --logger "trx"
```

**���ɲ���**:
```bash
dotnet test --filter "Category=Integration" --logger "trx"
```

## ? ��������˵��

### 1. ? JMeter���ܲ���
- **Ŀ��**: ��֤ϵͳ����ָ��
- **��������**: �����û�����Ӧʱ�䡢������
- **�ĵ�**: `JMeter���ܲ��Խű�����ָ��.md`
- **�ű�**: `BZK���ܲ���.jmx`

### 2. ? �����Զ�������
- **Ŀ��**: ��֤ϵͳ������ȷ��
- **��������**: ��Ԫ���ԡ����ɲ��ԡ�UI����
- **�ĵ�**: `�����Զ������Դ���.md`
- **���**: xUnit + Selenium + MSTest

### 3. ? �ֶ���������
- **Ŀ��**: ϵͳ�����ֶ�������֤
- **��������**: ��¼����ѯ��������Ȩ�޵�
- **�ĵ�**: `��ϸ���������ĵ�.md`
- **������**: 50+ ����ϸ���Գ���

### 4. ?? ��ȫ����
- **Ŀ��**: ��֤ϵͳ��ȫ��
- **��������**: OWASP Top 10��SQLע�롢XSS��
- **�ĵ�**: `��ȫ���Լ���嵥.md`
- **����**: OWASP ZAP���ֶ�����

## ? ���Ա���

### �Զ����ɱ���
ִ�в��Խű�����Զ�����HTML��ʽ���ۺϲ��Ա��棺
- **λ��**: `TestResults/TestReport.html`
- **����**: ����ͳ�ơ�������顢���ӵ�������Ա���

### ����ר���
- **JMeter����**: `TestResults/JMeter/html-report/index.html`
- **��Ԫ����**: `TestResults/Unit/`
- **���ɲ���**: `TestResults/Integration/`
- **UI����**: `TestResults/UI/`
- **��ȫ����**: `TestResults/Security/`

## ? ����˵��

### ������������
```csv
# users.csv - �����û�����
username,password
testuser01,Password123!
testuser02,Password123!
...
```

### ��������
```bash
# ϵͳ����URL
BASE_URL=http://localhost:5000

# ��������ã�UI���ԣ�
BROWSER=Chrome
HEADLESS=true

# ���ݿ����ӣ����ɲ��ԣ�
TEST_CONNECTION_STRING=Server=...
```

## ? ���Բ���

### ���Խ�����
```
    ? UI���� (����)
   -------- �˵��˹�����֤
  ?? ���ɲ��� (����)
 ------------ API�ͷ��񼯳�
??? ��Ԫ���� (����)
-------------- ҵ���߼���֤
```

### ���Էֲ�
1. **��Ԫ����**: ����ҵ���߼������ݷ��ʡ������
2. **���ɲ���**: ��֤API�ӿڡ����ݿ⽻�����ⲿ����
3. **UI����**: �ؼ��û����̵Ķ˵�����֤
4. **���ܲ���**: �����û�����Ӧʱ�䡢ϵͳ�ȶ���
5. **��ȫ����**: ©��ɨ�衢��͸���ԡ��������

## ? ���Լ���嵥

### ���ܲ��Լ��
- [ ] �û���¼����֤
- [ ] ��ѯ����������
- [ ] ���ݵ�������
- [ ] Ȩ�޿��ƻ���
- [ ] ������ͻָ�

### ���ܲ��Լ��
- [ ] �����û�֧�� (Ŀ��: 120�û�)
- [ ] ��ѯ��Ӧʱ�� (Ŀ��: <5��)
- [ ] �������ܲ��� (Ŀ��: 10K��<60��)
- [ ] ϵͳ�ȶ�����֤
- [ ] ��Դʹ�ü��

### ��ȫ���Լ��
- [ ] SQLע�����
- [ ] XSS��������
- [ ] Ȩ�޿�����֤
- [ ] �����������
- [ ] �Ự��ȫ��

## ? �����ų�

### ��������

**Q: JMeter���Խű��޷��ҵ�**
```
A: ȷ��JMeter�Ѱ�װ����ӵ�ϵͳPATH
   ��֤����: jmeter -v
```

**Q: ��Ԫ������Ŀδ�ҵ�**
```
A: ȷ��������Ŀ�ļ�������'.Tests.csproj'
   �����Ŀ�ṹ�������淶
```

**Q: ���ɲ�������ʧ��**
```
A: ȷ��Ŀ��ϵͳ��������
   ��֤URL: http://localhost:5000
   ������ǽ����������
```

**Q: UI����Chrome��������**
```
A: ����ChromeDriver����Chrome�汾ƥ��
   ������ȷ��PATH��������
```

### ���Խ���

1. **������ϸ��־**:
   ```powershell
   .\RunTests.ps1 -Verbose
   ```

2. **����ִ���������**:
   ```powershell
   .\RunTests.ps1 -TestType Unit
   ```

3. **���ϵͳ״̬**:
   ```bash
   curl -I http://localhost:5000/health
   ```

4. **�鿴��ϸ����**:
   ```bash
   dotnet test --logger "console;verbosity=detailed"
   ```

## ? ����ĵ�

### ��Ŀ�ĵ�
- [ϵͳ������˵����](../02-�����ĵ�/ϵͳ������˵����.md)
- [ϵͳ�ܹ�����ĵ�](../02-�����ĵ�/ϵͳ�ܹ�����ĵ�.md)
- [��ҵ����Ŀ�ĵ�����](../��ҵ����Ŀ�ĵ�����.md)

### �ⲿ��Դ
- [Apache JMeter�û��ֲ�](https://jmeter.apache.org/usermanual/index.html)
- [xUnit.net�ĵ�](https://xunit.net/docs/getting-started/netcore/cmdline)
- [Selenium WebDriver�ĵ�](https://selenium-python.readthedocs.io/)
- [OWASP����ָ��](https://owasp.org/www-project-web-security-testing-guide/)

## ? ֧���뷴��

### ��ϵ��ʽ
- **����֧��**: �����Ŷ�
- **����֧��**: ���Թ���ʦ
- **�ĵ�ά��**: �����ĵ��Ŷ�

### ���ⱨ��
���ڷ�������ʱ�ṩ������Ϣ��
1. ����ϵͳ�Ͱ汾
2. �������ͺ;��峡��
3. ������Ϣ����־
4. ���ֲ���
5. �������

---

**�ĵ�ά��**: ���Թ���ʦ  
**��������**: 2025��1��3��  
**���ð汾**: BZK��ѯϵͳ v2.0+  
**���֤**: �ڲ��ĵ� 