using GostCryptography.Cryptography;
using GostCryptography.Xml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Schema;
using gisgmp_signer.Properties;
using System.Threading;

namespace gisgmp_signer
{
    class Signer
    {
        private const string WsSecurityExtNamespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
        private const string WsSecurityUtilityNamespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";
        private const string EnvelopNamespace = "http://schemas.xmlsoap.org/soap/envelope/";
        private Settings settings = Settings.Default;

        private string[,] GisGmpNamespaces =
        {
            // сущности
            { "Charge", "http://roskazna.ru/gisgmp/xsd/116/Charge" },
            { "FinalPayment", "http://roskazna.ru/gisgmp/xsd/116/PaymentInfo" },
            // запросы
            { "ExportRequest", "http://roskazna.ru/gisgmp/xsd/116/MessageData" },
            { "DoAcknowledgmentRequest", "http://roskazna.ru/gisgmp/xsd/116/Message" },
            { "ChargeCreationRequest", "http://roskazna.ru/gisgmp/xsd/116/Message" },
        };

        private X509Certificate2 certOV;
        private X509Certificate2 certSP;
        private XmlDocument file = new XmlDocument();
        private StreamWriter logFile;
        public string inFileName;
        public string outFileName;

        public bool logToRtb = false;

        public Signer(string inFile, string outFile = "")
        {
             InitFiles(inFile, outFile);
            InitCerts();
        }

        public Signer()
        {
            try
            {
                InitCerts();
            }
            catch (Exception E)
            {
                throw E;
            }
        }

        private void Log(string str)
        {
            Logger.Instance.Log(str);
        }

        private void InitLog(string inFile)
        {
            string l = inFile != String.Empty ? Path.GetFileNameWithoutExtension(inFile) : "log";
            string logFileName = l + DateTime.Now.ToString("_yyyyMMdd_HHmmss") + ".log";
            l = settings.FolderOut != String.Empty && Directory.Exists(settings.FolderOut) ? settings.FolderOut + Path.DirectorySeparatorChar + l : logFileName;
            logFile = new StreamWriter(l, false, Encoding.UTF8);
            Log(" начинаем логирование ");
        }

        private void InitFiles(string inFile, string outFile)
        {
            string fullFileName;

            if (inFile != String.Empty)
            {
                if (Path.GetDirectoryName(inFile) == "")
                {
                    fullFileName = settings.FolderIn + Path.DirectorySeparatorChar + inFile;
                }
                else
                {
                    fullFileName = inFile;
                }
                if (!File.Exists(fullFileName))
                {
                    throw new Exception("Указанный файл не найден");
                }
                inFileName = fullFileName;

                if (outFile != String.Empty)
                {
                    if (Path.GetDirectoryName(outFile) == "")
                    {
                        fullFileName = settings.FolderOut + Path.DirectorySeparatorChar + outFile;
                    }
                    else
                    {
                        fullFileName = outFile;
                    }
                }
                else
                {
                    string fileNameWithoutExt = Path.GetFileNameWithoutExtension(inFile);
                    if (Path.GetDirectoryName(inFile) == "")
                    {
                        fullFileName = Settings.Default.FolderOut + Path.DirectorySeparatorChar + fileNameWithoutExt + "_signed.xml";
                    }
                    else
                    {
                        fullFileName = fileNameWithoutExt + "_signed.xml";
                    }
                    while (File.Exists(fullFileName))
                    {
                        Regex regex = new Regex(@"^(.*_)\((\d*)\)(\.xml)$");
                        if (regex.IsMatch(fullFileName))
                        {
                            Match match = regex.Match(fullFileName);
                            fullFileName = regex.Replace(fullFileName, "$1(" + (Convert.ToInt32(match.Groups[2].Value) + 1).ToString() + ")$3");
                        } else
                        {
                            fullFileName = Regex.Replace(fullFileName, @"^(.*)(\.xml)$", "$1_(1)$2");
                        }
                    }
                }
                outFileName = fullFileName;
            }
            else
            {
                throw new Exception("Не указан файл для подписи");
            }
        }

        private X509Certificate2 GetCertBySerialNumber(string serialNumber)
        {
            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);

            serialNumber = (new Regex("[^a-fA-F0-9]")).Replace(serialNumber, string.Empty).ToUpper();

            X509Certificate2Collection certs = store.Certificates.Find(X509FindType.FindBySerialNumber, serialNumber, true);

            try
            {
                if (certs.Count > 0)
                {
                    return certs[0];
                }

            }
            finally
            {
                store.Close();
            }
            return null;
        }

        private bool IsNormalCert(X509Certificate2 cert, string serialNumber)
        {
            serialNumber = (new Regex("[^a-fA-F0-9]")).Replace(serialNumber, string.Empty).ToUpper();

            if (cert != null && cert.GetSerialNumberString() == serialNumber)
            {
                if (cert.SignatureAlgorithm.Value == "1.2.643.2.2.3")
                {
                    if (cert.HasPrivateKey)
                    {
                        return true;
                    }
                    else
                    {
                        throw new Exception("Не найден закрытый ключ сертификата");
                    }
                }
                else
                {
                    throw new Exception("Алгоритм сертификата должен быть ГОСТ Р 34.10/34.11-2001 (1.2.643.2.2.3)");
                }
            }
            else
            {
                throw new Exception("Сертификат не найден (или срок его действия истек)");
            }
        }

        private bool IsVipNetCert(X509Certificate2 cert)
        {
            CspParameters PrivateKeyInfo = cert.GetPrivateKeyInfo();

            if (cert.SignatureAlgorithm.Value == "1.2.643.2.2.3")
            {
                if (cert.HasPrivateKey)
                {
                    if (PrivateKeyInfo.ProviderName == "Infotecs Cryptographic Service Provider")
                    {
                        return true;
                    }
                    else
                    {
                        throw new Exception("Криптопровайдер должен быть VipNet. Получен: [" + PrivateKeyInfo.ProviderName + "]");
                    }
                }
                else
                {
                    throw new Exception("Не найден закрытый ключ сертификата");
                }
            }
            else
            {
                throw new Exception("Алгоритм сертификата должен быть ГОСТ Р 34.10/34.11-2001 (1.2.643.2.2.3)");
            }
        }

        private bool IsCryptoProCert(X509Certificate2 cert)
        {
            CspParameters PrivateKeyInfo = cert.GetPrivateKeyInfo();

            if (cert.SignatureAlgorithm.Value == "1.2.643.2.2.3")
            {
                if (cert.HasPrivateKey)
                {
                    if (PrivateKeyInfo.ProviderName == "Crypto-Pro GOST R 34.10-2001 Cryptographic Service Provider")
                    {
                        return true;
                    }
                    else
                    {
                        throw new Exception("Криптопровайдер должен быть CryptoPro. Получен: [" + PrivateKeyInfo.ProviderName + "]");
                    }
                }
                else
                {
                    throw new Exception("Не найден закрытый ключ сертификата");
                }
            }
            else
            {
                throw new Exception("Алгоритм сертификата должен быть ГОСТ Р 34.10/34.11-2001 (1.2.643.2.2.3)");
            }
        }

        private bool IsNormalCerts(X509Certificate2 certOV, X509Certificate2 certSP)
        {
            CspParameters PrivateKeyInfoOV = certOV.GetPrivateKeyInfo();
            CspParameters PrivateKeyInfoSP = certSP.GetPrivateKeyInfo();

            if (PrivateKeyInfoOV.ProviderName == PrivateKeyInfoSP.ProviderName)
            {
                if (certOV.SignatureAlgorithm.Value == "1.2.643.2.2.3")
                {
                    if (certOV.HasPrivateKey)
                    {
                        if ((PrivateKeyInfoOV.ProviderName.Equals("Crypto-Pro GOST R 34.10-2001 Cryptographic Service Provider")) ||
                            (PrivateKeyInfoOV.ProviderName.Equals("Infotecs Cryptographic Service Provider")))
                        {
                            return true;
                        }
                        else
                        {
                            throw new Exception("Выбраны сертификаты с неподдерживаемым криптопровайдером");
                        }
                    }
                    else
                    {
                        throw new Exception("Не найден закрытый ключ сертификата");
                    }
                }
                else
                {
                    throw new Exception("Алгоритм сертификата должен быть ГОСТ Р 34.10/34.11-2001 (1.2.643.2.2.3)");
                }
            }
            else
            {
                throw new Exception("Криптопровайдеры сертификатов не совпадают");
            }
        }

        private int GetProviderTypeByCert(X509Certificate2 cert)
        {
            CspParameters PrivateKeyInfo = cert.GetPrivateKeyInfo();

            if (PrivateKeyInfo.ProviderName == "Crypto-Pro GOST R 34.10-2001 Cryptographic Service Provider")
            {
                return ProviderTypes.CryptoPro;
            }
            else if (PrivateKeyInfo.ProviderName == "Infotecs Cryptographic Service Provider")
            {
                return ProviderTypes.VipNet;
            }
            else
            {
                throw new Exception("Выбраны сертификаты с неподдерживаемым криптопровайдером");
            }
        }

        private void InitCerts()
        {
            Log("инициализация сертификатов");
            try
            {
                if (Settings.Default.DSOV != String.Empty)
                    certOV = GetCertBySerialNumber(Settings.Default.DSOV);
                else
                    throw new Exception("Не указан серийный номер сертификата для ЭП-ОВ");
                if (certOV == null)
                    throw new Exception("Сертификат для ЭП-ОВ не найден в хранилище сертификатов");

                if (Settings.Default.DSSP != String.Empty)
                    certSP = GetCertBySerialNumber(Settings.Default.DSSP);
                else
                    throw new Exception("Не указан серийный номер сертификата для ЭП-СП");
                if (certOV == null)
                    throw new Exception("Сертификат для ЭП-СП не найден в хранилище сертификатов");

                switch (Settings.Default.CryptoProv)
                {
                    case ("VipNet"):
                        if (IsVipNetCert(certOV) && IsVipNetCert(certSP))
                        {
                            GostCryptoConfig.ProviderType = ProviderTypes.VipNet;
                        };
                        break;
                    case ("CryptoPro"):
                        if (IsCryptoProCert(certOV) && IsCryptoProCert(certSP))
                        {
                            GostCryptoConfig.ProviderType = ProviderTypes.CryptoPro;
                        };
                        break;
                    default:
                        if (IsNormalCerts(certOV, certSP))
                        {
                            GostCryptoConfig.ProviderType = GetProviderTypeByCert(certOV);
                        }
                        break;
                }
                Log(" ok");
            }
            catch (Exception e)
            {
                Log(e.Message);
                throw e;
            }
        }

        public bool SignFile()
        {
            Log("подписание файла " + inFileName);
            try
            {
                file.Load(inFileName);

                // Удаляем тег wsse:Security если есть
                XmlNodeList sec = file.GetElementsByTagName("Header", EnvelopNamespace);
                if (sec.Count != 0)
                {
                    sec[0].ParentNode.RemoveChild(sec[0]);
                }

                // ищем места для подписи
                XmlNodeList signNodes = GetSignaturesParent();
                if (signNodes.Count == 0)
                {
                    signNodes = GetRequestNodes();
                    if (signNodes.Count == 0)
                    {
                        Log(" не найдено место для подписания, файл пропущен");
                        return false;
                    }
                }

                // подписываем сущности/запросы
                SignGisGpm(signNodes);

                // подписываем заголовок
                SignHeaderSmev();

                // сохраняем файл
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Encoding = new UTF8Encoding(false);
                XmlWriter xw = XmlWriter.Create(outFileName, settings);
                file.WriteTo(xw);
                xw.Close();

                Log(" подписание произошло без критических ошибок");
                return true;
            }
            catch (Exception e)
            {
                Log(e.Message);
                return false;
            }
        }

        public bool SignFile(string inFile)
        {
            InitFiles(inFile, inFile);
            return SignFile();
        }

        public void SignFileAndMoveIt(string fileName)
        {
            try
            {
                if (SignFile(Path.GetFileName(fileName)))
                { // файл подписался
                    File.Delete(Settings.Default.FolderIn + Path.DirectorySeparatorChar + Path.GetFileName(fileName));
                }
                else
                {
                    string newFileName = Settings.Default.FolderError + Path.DirectorySeparatorChar + Path.GetFileName(fileName);
                    while (File.Exists(newFileName))
                    {
                        Regex regex = new Regex(@"^(.*_)\((\d*)\)(\.xml)$");
                        if (regex.IsMatch(newFileName))
                        {
                            Match match = regex.Match(newFileName);
                            newFileName = regex.Replace(newFileName, "$1(" + (Convert.ToInt32(match.Groups[2].Value) + 1).ToString() + ")$3");
                        }
                        else
                        {
                            newFileName = Regex.Replace(newFileName, @"^(.*)(\.xml)$", "$1_(1)$2");
                        }
                    }
                    File.Move(fileName, newFileName);
                }
            }
            catch (Exception e)
            {
                Log(e.Message);
            }
        }

        public void SignAllFiles(string path)
        {
            string[] files = Directory.GetFiles(path, "*.xml");

            if (files.Length > 0)
            {
                for (int i = 0; i < files.Length; i++)
                {
                    SignFileAndMoveIt(files[i]);
                }
            }
        }

        private XmlNodeList GetSignaturesParent()
        {
            var namespaceManager = new XmlNamespaceManager(file.NameTable);
            namespaceManager.AddNamespace("ds", SignedXml.XmlDsigNamespaceUrl);

            XmlNodeList nodes = file.SelectNodes("//ds:Signature/..", namespaceManager);

            foreach (XmlNode n in nodes)
                n.RemoveChild(file.SelectSingleNode("//ds:Signature", namespaceManager));

            return nodes;
        }

        private XmlNodeList GetRequestNodes()
        {
            XmlNodeList nodes = null;
            for (int i = 0; i < GisGmpNamespaces.GetLength(0); i++)
            {
                nodes = file.GetElementsByTagName(GisGmpNamespaces[i, 0], GisGmpNamespaces[i, 1]);
                if (nodes.Count != 0) break;
            }
            return nodes;
        }

        private void SignGisGpm(XmlNodeList signNodeList)
        {
            if (signNodeList.Count != 0)
            {
                Log(" подписание сущности/запроса");
                var namespaceManager = new XmlNamespaceManager(file.NameTable);
                namespaceManager.AddNamespace("ds", SignedXml.XmlDsigNamespaceUrl);

                foreach (XmlElement signNode in signNodeList)
                {
                    // получаем ID подписываемого документа
                    string refId = signNode.GetAttribute("Id");

                    // Создание подписчика XML-документа
                    var signedXml = new GostSignedXml(file) /*{ GetIdElementHandler = GetSmevIdElement }*/;

                    // Установка ключа для создания подписи
                    signedXml.SetSigningCertificate(certOV);

                    // Ссылка на узел, который нужно подписать, с указанием алгоритма хэширования ГОСТ Р 34.11-94 (в соответствии с методическими рекомендациями СМЭВ)
                    var dataReference = new Reference { Uri = "#" + refId, DigestMethod = GostSignedXml.XmlDsigGost3411ObsoleteUrl };

                    // Методы преобразования, применяемый к данным перед их подписью
                    dataReference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
                    dataReference.AddTransform(new XmlDsigExcC14NTransform());

                    // Установка ссылки на узел
                    signedXml.AddReference(dataReference);

                    // Установка алгоритма нормализации узла SignedInfo (в соответствии с методическими рекомендациями СМЭВ)
                    signedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigExcC14NTransformUrl;

                    // Установка алгоритма подписи ГОСТ Р 34.10-2001 (в соответствии с методическими рекомендациями СМЭВ)
                    // signedXml.SignedInfo.SignatureMethod = GostSignedXml.XmlDsigGost3410ObsoleteUrl;

                    // Установка информации о сертификате, который использовался для создания подписи
                    var keyInfo = new KeyInfo();
                    keyInfo.AddClause(new KeyInfoX509Data(certOV));
                    signedXml.KeyInfo = keyInfo;

                    // Вычисление подписи
                    signedXml.ComputeSignature("ds"); // "ds"

                    // Получение XML-представления подписи
                    var signatureXml = signedXml.GetXml("ds");

                    // Добавление подписи в исходный документ
                    file.SelectSingleNode("//*[@Id='" + refId + "']").AppendChild(file.ImportNode(signatureXml, true));

                    // Проверим подпись
                    // VerifSign(file);
                }

                Log(VerifGisGmpSign() ? "   подпись корректна" : "  !подпись некорректна");
            }
            else
            {
                // log(" сущность/запрос не обнаружен");
            }
        }

        private bool VerifGisGmpSign()
        {
            // Создание подписчика XML-документа
            var signedXml = new GostSignedXml(file) { GetIdElementHandler = GetSmevIdElement };

            // Поиск узла с подписью
            var nodeList = file.GetElementsByTagName("Signature", SignedXml.XmlDsigNamespaceUrl);

            // Загрузка найденной подписи
            signedXml.LoadXml((XmlElement)nodeList[0]);

            return signedXml.CheckSignature();
        }

        private static XmlElement GetSmevIdElement(XmlDocument document, string idValue)
        {
            var namespaceManager = new XmlNamespaceManager(document.NameTable);
            namespaceManager.AddNamespace("wsu", WsSecurityUtilityNamespace);

            return document.SelectSingleNode("//*[@wsu:Id='" + idValue + "']", namespaceManager) as XmlElement;
        }

        private void BooksSettingsValidationEventHandler(object sender, ValidationEventArgs e)
        {
            if (e.Severity == XmlSeverityType.Warning)
            {
                throw new Exception("WARNING: " + e.Message);
            }
            else if (e.Severity == XmlSeverityType.Error)
            {
                throw new Exception("ERROR: " + e.Message);
            }
            throw new Exception(e.Message);
        }

        private static bool VerifySmevRequestSignature(XmlDocument signedSmevRequest)
        {
            // Создание подписчика XML-документа
            var signedXml = new GostSignedXml(signedSmevRequest) { GetIdElementHandler = GetSmevIdElement };

            // Поиск узла с подписью
            var nodeList = signedSmevRequest.GetElementsByTagName("Signature", SignedXml.XmlDsigNamespaceUrl);

            // Загрузка найденной подписи
            signedXml.LoadXml((XmlElement)nodeList[0]);

            // Поиск ссылки на BinarySecurityToken
            var references = signedXml.KeyInfo.GetXml().GetElementsByTagName("Reference", WsSecurityExtNamespace);

            if (references.Count > 0)
            {
                // Определение ссылки на сертификат (ссылка на узел документа)
                var binaryTokenReference = ((XmlElement)references[0]).GetAttribute("URI");

                if (!String.IsNullOrEmpty(binaryTokenReference) && binaryTokenReference[0] == '#')
                {
                    // Поиск элемента с закодированным в Base64 сертификатом
                    var binaryTokenElement = signedXml.GetIdElement(signedSmevRequest, binaryTokenReference.Substring(1));

                    if (binaryTokenElement != null)
                    {
                        // Загрузка сертификата, который был использован для подписи
                        var signingCertificate = new X509Certificate2(Convert.FromBase64String(binaryTokenElement.InnerText));

                        // Проверка подписи
                        return signedXml.CheckSignature(signingCertificate.GetPublicKeyAlgorithm());
                    }
                }
            }

            return false;
        }

        private void SignHeaderSmev()
        {
            Log(" подписание заголовка");
            XmlDocument signNode = new XmlDocument();
            signNode.InnerXml = Resources.SMEVHeader;

            // Создание подписчика XML-документа
            var signedXml = new GostSignedXml(file) { GetIdElementHandler = GetSmevIdElement };

            // Установка ключа для создания подписи
            signedXml.SetSigningCertificate(certSP);

            // Ссылка на узел, который нужно подписать, с указанием алгоритма хэширования ГОСТ Р 34.11-94 (в соответствии с методическими рекомендациями СМЭВ)
            var dataReference = new Reference { Uri = "#body", DigestMethod = GostSignedXml.XmlDsigGost3411ObsoleteUrl };

            // Метод преобразования, применяемый к данным перед их подписью (в соответствии с методическими рекомендациями СМЭВ)
            //            dataReference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
            dataReference.AddTransform(new XmlDsigExcC14NTransform());

            // Установка ссылки на узел
            signedXml.AddReference(dataReference);

            // Установка алгоритма нормализации узла SignedInfo (в соответствии с методическими рекомендациями СМЭВ)
            signedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigExcC14NTransformUrl;

            // Установка алгоритма подписи ГОСТ Р 34.10-2001 (в соответствии с методическими рекомендациями СМЭВ)
            signedXml.SignedInfo.SignatureMethod = GostSignedXml.XmlDsigGost3410ObsoleteUrl;

            // Вычисление подписи
            signedXml.ComputeSignature("ds");

            // Получение XML-представления подписи
            var signatureXml = signedXml.GetXml("ds");

            // Добавление подписи в исходный документ
            signNode.GetElementsByTagName("ds:SignedInfo")[0].ParentNode.RemoveChild(signNode.GetElementsByTagName("ds:SignedInfo")[0]);
            signNode.GetElementsByTagName("ds:SignatureValue")[0].InnerText = signatureXml.GetElementsByTagName("ds:SignatureValue")[0].InnerText;
            signNode.GetElementsByTagName("ds:Signature")[0].PrependChild(signNode.ImportNode(signatureXml.GetElementsByTagName("ds:SignedInfo")[0], true));
            signNode.GetElementsByTagName("wsse:BinarySecurityToken")[0].InnerText = Convert.ToBase64String(certSP.RawData);

            file.DocumentElement.PrependChild(file.CreateElement(file.DocumentElement.Prefix, "Header", "http://schemas.xmlsoap.org/soap/envelope/"));
            file.GetElementsByTagName(file.DocumentElement.Prefix + ":Header")[0].PrependChild(file.ImportNode(signNode.GetElementsByTagName("wsse:Security")[0], true));

            Log(VerifySmevRequestSignature(file) ? "   подпись корректна" : "  !подпись некорректна");
        }


    }
}
