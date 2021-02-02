using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Gradery
{
    internal static class Program
    {
        static void Main()
        {
            GraderyController.GetInstance().LoadApp();

            Console.WriteLine();
            Console.WriteLine("Programmende. Please press the any-key...");
            Console.ReadKey();
        }
    }

    #region Appointment

    /// <summary>
    /// Template for an upcoming Appointment.
    /// </summary>
    [Serializable]
    internal class Appointment
    {
        internal string Name { get; private set; }
        internal readonly DateTime DateValue;
        internal readonly DateTime DateEndValue;
        internal static readonly Random _Random = new Random();

        internal Appointment(string pName, DateTime pDate)
        {
            Name = pName;
            DateValue = pDate;
        }

        internal Appointment(string pName, DateTime pStartDate, DateTime pEndDate)
        {
            Name = pName;
            DateValue = pStartDate;
            DateEndValue = pEndDate;
        }

        internal string GetSerializeString()
        {
            // https://icalendar.org/validator.html
            // https://tools.ietf.org/html/rfc5545#page-146
            var timestamp = DateTime.Now.ToString("yyyyMMddTHHmmssZ");
            StringBuilder iCal = new StringBuilder();
            iCal.AppendLine("BEGIN:VCALENDAR");
            iCal.AppendLine("VERSION:2.0");
            iCal.AppendLine("PRODID:-//hacksw/handcal//NONSGML v1.0//EN");
            iCal.AppendLine("BEGIN:VEVENT");
            iCal.AppendLine("UID:" + timestamp + "_" + _Random.Next(123456789) + "@gradery.de");
            iCal.AppendLine("DTSTAMP:" + timestamp);
            iCal.AppendLine("DTSTART:" + DateValue.ToString("yyyyMMddTHHmmssZ"));
            iCal.AppendLine("DTEND:" + DateValue.ToString("yyyyMMddTHHmmssZ"));
            iCal.AppendLine("SUMMARY:" + Name);
            iCal.AppendLine("END:VEVENT");
            iCal.AppendLine("END:VCALENDAR");
            return iCal.ToString();
        }

        public override string ToString()
        {
            return string.Format("Termin={0}\nDatum={1}\nEnddatum={2}",
                Name, DateValue.ToShortDateString(), DateEndValue.ToShortDateString());
        }
    }

    #endregion

    #region CryptoHandler with string extension methods.

    internal static class CryptoHandler
    {
        internal static string Decrypt(this string pStringToDecrypt, int pKey)
        {
            if (pStringToDecrypt.Length % pKey == 0)
            {
                var newKey = pStringToDecrypt.Length / pKey;
                var listSubStringCharArrays = new List<char[]>();
                for (var i = 0; i < pStringToDecrypt.Length; i += newKey)
                    listSubStringCharArrays.Add(pStringToDecrypt.Substring(i, newKey).ToCharArray());

                var decryptedString = "";
                for (var column = 0; column < newKey; column++)
                {
                    for (var row = 0; row < pKey; row++)
                    {
                        if (listSubStringCharArrays[row][column] == '$')
                            continue;
                        decryptedString += listSubStringCharArrays[row][column];
                    }
                }
                return decryptedString;
            }
            else
                Console.WriteLine("Laenge des zu entschlüsselnden Textes ungültig.");
            return "";
        }

        // https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/extension-methods
        internal static string Encrypt(this string pStringToEncrypt, int pKey)
        {
            while (pStringToEncrypt.Length % pKey != 0)
                pStringToEncrypt += "$";

            var listSubStringCharArrays = new List<char[]>();
            for (int i = 0; i < pStringToEncrypt.Length; i += pKey)
                listSubStringCharArrays.Add(pStringToEncrypt.Substring(i, pKey).ToCharArray());

            var newKey = pStringToEncrypt.Length / pKey;
            var encryptedString = "";
            for (var column = 0; column < pKey; column++)
                for (var row = 0; row < newKey; row++)
                    encryptedString += listSubStringCharArrays[row][column];
            return encryptedString;
        }
    }

    #endregion

    #region ICertifiable

    // Requirement for all Grade-types.
    interface ICertifiable
    {
        void Certify();
    }

    #endregion

    #region CompositeCertificateGrades

    class CompositeCertificateGrades : ICertifiable
    {
        private readonly List<ICertifiable> ListOfICertifiable = new List<ICertifiable>();

        public void AddCertifiable(ICertifiable pCertifiable)
        {
            ListOfICertifiable.Add(pCertifiable);
        }

        public void RemoveCertifiable(ICertifiable pCertifiable)
        {
            ListOfICertifiable.Remove(pCertifiable);
        }

        public void Certify()
        {
            foreach (var certifiable in ListOfICertifiable)
                certifiable.Certify();
        }
    }

    #endregion

    #region Grade

    /// <summary>
    /// Grade.
    /// </summary>
    /// <remarks>
    /// Class and it's members must be public in order to be serializable.
    /// It also implements an interface and so cannot be less accessible.
    /// </remarks>
    [Serializable()]
    public class Grade : ISerializable
    {
        public string Date { get; set; }
        public SchoolSubject SchoolSubject;
        public string Type { get; set; }
        public double Value { get; set; }

        public Grade()
        {
            // Parameterless constructor needed by the XML-Serializer.
        }

        protected Grade(SerializationInfo info, StreamingContext context)
        {
            SchoolSubject = (SchoolSubject)info.GetValue("Schulfach", typeof(SchoolSubject));
            Value = (int)info.GetValue("Notenwert", typeof(int));
            Type = (string)info.GetValue("Bezeichnung", typeof(string));
            Date = (string)info.GetValue("DatumKurz", typeof(string));
        }

        public Grade(DateTime pDate, SchoolSubject pSchoolSubject, string pType, float pValue)
        {
            Date = Convert.ToString(pDate.ToShortDateString());
            SchoolSubject = pSchoolSubject;
            Type = pType;
            Value = pValue;
        }

        public string GetEncryptedSerializeString(int pKey)
        {
            return string.Format("Schulfach={0};Datum={1};Bezeichnung={2};Notenwert={3};#",
                SchoolSubject, Date, Type, Value).Encrypt(pKey);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Schulfach", SchoolSubject);
            info.AddValue("Notenwert", Value);
            info.AddValue("Bezeichnung", Type);
            info.AddValue("DatumKurz", Date);
        }

        public string GetSerializeString()
        {
            return string.Format("Schulfach={0};Datum={1};Bezeichnung={2};Notenwert={3};#",
                SchoolSubject, Date, Type, Value);
        }

        public string GetWordGrade()
        {
            // 100-92
            if (Value >= 1.0 && Value <= 1.4)
                return "sehr gut";
            // 91-81
            else if (Value >= 1.5 && Value <= 2.4)
                return "gut";
            // 80-67
            else if (Value >= 2.5 && Value <= 3.4)
                return "befriedigend";
            // 66-50
            else if (Value >= 3.5 && Value <= 4.4)
                return "ausreichend";
            // 49-30
            else if (Value >= 4.5 && Value <= 5.4)
                return "mangelhaft";
            // 29-0
            else if (Value >= 5.5 && Value <= 6.0)
                return "ungenügend";
            else
                return "Wortnote konnte nicht bestimmt werden.";
        }

        public override string ToString()
        {
            return string.Format("Schulfach={0}\nDatum={1}\nBezeichnung={2}\nNotenwert={3}\n",
                SchoolSubject, Date, Type, Value);
        }
    }

    #endregion

    #region SubGrade

    /// <summary>
    /// Teilleistung (Klausur oder SoMi).
    /// </summary>
    [Serializable()]
    public class SubGrade : Grade
    {
        public List<Grade> Grades = new List<Grade>();
        public float Weighting { get; set; }

        public SubGrade()
        {

        }

        protected SubGrade(SerializationInfo info, StreamingContext context)
        {
            Grades = (List<Grade>)info.GetValue("Notenliste", typeof(List<Grade>));
            Weighting = (float)info.GetValue("Notengewichtung", typeof(float));
        }
    }

    #endregion

    #region CertificateGrade

    [Serializable()]
    public class CertificateGrade : Grade, ICertifiable
    {
        public CertificateGrade()
        {

        }

        protected CertificateGrade(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            SchoolSubject = (SchoolSubject)info.GetValue("Schulfach", typeof(SchoolSubject));
            Value = (int)info.GetValue("Notenwert", typeof(int));
        }

        public void Certify()
        {
            Console.WriteLine(string.Format("The certificate grade for {0} will be: '{1}' ({2})",
                SchoolSubject, GetWordGrade(), Value));
        }
    }

    #endregion

    #region GraderyView

    /// <summary>
    /// View for the Gradery main section.
    /// </summary>
    internal class GraderyView : View
    {
        internal GraderyView()
        {
            GraderyController.GetInstance().BeforeNextActionEvent += GraderyView_BeforeNextActionEvent;
        }

        private void GraderyView_BeforeNextActionEvent()
        {
            Console.Clear();
            Console.WriteLine("Loading...");
            // Simulate some background work...
            Thread.Sleep(3000);
            GraderyController.GetInstance().ShowLogo();
        }

        internal override void LoadContext(User user)
        {
            GraderyController.GetInstance().ShowLogo();
            Console.WriteLine("1. Note eingeben");
            Console.WriteLine("2. Note(n) anzeigen");
            Console.WriteLine("3. Termin eingeben");
            Console.WriteLine("4. Termin(e) anzeigen");
            Console.WriteLine("5. Zeugnisnote(n) berechnen");
            Console.Write("Auswahl: ");
            GraderyController.GetInstance().NavigateToAction((Student)user);
            GraderyController.GetInstance().NavigateAfterAction((Student)user, this);
        }
    }

    #endregion

    #region GraderyController

    /// <summary>
    /// Singleton that handles the internal processes.
    /// </summary>
    internal sealed class GraderyController
    {
        // Einzige Instanz der Klasse deklarieren
        private static GraderyController Instance;
        // Objekt im aktuellen Thread sperren
        private static readonly object LockThis = new object();

        private readonly SerializationController serializationController = new SerializationController();

        private readonly LoginView loginView = new LoginView();

        internal delegate void BeforeNextAction();
        // https://www.tutorialsteacher.com/csharp/csharp-event
        internal event BeforeNextAction BeforeNextActionEvent;

        private readonly CompositeCertificateGrades CertificateGrades = new CompositeCertificateGrades();

        private GraderyController() { }

        /// <summary>
        /// Einzige Instanz der Klasse initialisieren, sofern noch null
        /// </summary>
        /// <returns>Instanz der Klasse</returns>
        internal static GraderyController GetInstance()
        {
            // Die lock-Anweisung ruft die Sperre für gegenseitigen Ausschluss für ein bestimmtes Objekt ab,
            // https://docs.microsoft.com/de-de/dotnet/csharp/language-reference/keywords/lock-statement
            lock (LockThis)
                // führt einen Anweisungsblock aus...
                if (Instance == null)
                    // und hebt die Sperre anschließend auf
                    Instance = new GraderyController();
            return Instance;
        }

        internal void AddAppointment(Student pStudent)
        {
            BeforeNextActionEvent.Invoke();

            Appointment appointment = pStudent.GetAppointmentFromUserInput();
            if (appointment != null)
                serializationController.SerializeAppointment(appointment);
            else
                Console.WriteLine("Eingabefehler!");
        }

        internal void AddGrade(Student pStudent)
        {
            BeforeNextActionEvent.Invoke();

            Grade grade = pStudent.GetSubGradeFromUserInput();
            if (grade != null)
                serializationController.SerializeGrade(pStudent, grade);
            else
                Console.WriteLine("Eingabefehler!");
        }

        internal void CalculateCertificateGrade(Student student)
        {
            BeforeNextActionEvent.Invoke();

            var aweSubGradeKlausuren = new SubGrade
            {
                // Staerkere Gewichtung.
                Weighting = 0.6f
            };
            aweSubGradeKlausuren.Grades.Add(new Grade(new DateTime(07 / 05 / 2020), SchoolSubject.AWE, "Klausur", 1.3f));
            aweSubGradeKlausuren.Grades.Add(new Grade(new DateTime(07 / 06 / 2020), SchoolSubject.AWE, "Klausur", 2.3f));
            aweSubGradeKlausuren.Grades.ForEach(x => { aweSubGradeKlausuren.Value += x.Value; });

            var aweSubGradeSoMi = new SubGrade
            {
                // Schwaechere Gewichtung
                Weighting = 0.4f
            };
            aweSubGradeSoMi.Grades.Add(new Grade(new DateTime(07 / 05 / 2020), SchoolSubject.AWE, "SoMi", 1.3f));
            aweSubGradeSoMi.Grades.ForEach(x => { aweSubGradeSoMi.Value += x.Value; });

            // AWE-Note bestimmen...
            ICertifiable aweCertificateGrade = new CertificateGrade()
            {
                SchoolSubject = SchoolSubject.AWE,
                // TODO: Gewichtungsfaktor beruecksichtigen
                // TODO: Note abrunden und gem. IHK-Notenschluessel auf ausgeschriebenen Wert festlegen
                // https://docs.microsoft.com/de-de/dotnet/api/system.math.round?view=netframework-4.7.2
                Value = Math.Round(unchecked(
                    unchecked(aweSubGradeKlausuren.Value / aweSubGradeKlausuren.Grades.Count * aweSubGradeKlausuren.Weighting)
                    + unchecked(aweSubGradeSoMi.Value / aweSubGradeSoMi.Grades.Count * aweSubGradeSoMi.Weighting)), 1)
            };
            // ...dem Zeugnis hinzufuegen...
            CertificateGrades.AddCertifiable(aweCertificateGrade);
            // ...und alle Noten bescheinigen.
            CertificateGrades.Certify();
        }

        internal void CenterText(string pTextToCenter)
        {
            Console.Write(new string(' ', (Console.WindowWidth - pTextToCenter.Length) / 2));
            Console.WriteLine(pTextToCenter);
        }

        internal void DeserializeAppointment(Student pStudent)
        {
            Console.WriteLine(serializationController.DeSerializeAppointmentFromIcs(pStudent).ToString());
        }

        internal void DeserializeGrade(Student pStudent)
        {
            Console.WriteLine(serializationController.DeSerializeGradeFromBinary(pStudent).ToString());
            Console.WriteLine(serializationController.DeSerializeGradeFromJson(pStudent).ToString());
            // Decrypt Grade(s) from ".txt" with a (known) key of 3.
            Console.WriteLine(serializationController.DeSerializeGradeFromTxt(pStudent, 3).ToString());
            Console.WriteLine(serializationController.DeSerializeGradeFromXml(pStudent).ToString());
        }

        internal void LoadApp()
        {
            loginView.LoadContext(new User());
        }

        internal void NavigateAfterAction(User user, View view)
        {
            Console.WriteLine("Zurück zum Hauptmenü (H) - Ausloggen (A)");
            switch (Console.ReadKey(true).Key)
            {
                // Navigate to Login view.
                case ConsoleKey.A:
                    loginView.LoadContext(new User());
                    break;
                // Navigate to Gradery View.
                case ConsoleKey.H:
                    view.LoadContext(user);
                    break;
            }
        }

        internal void NavigateToAction(Student pStudent)
        {
            int pressedKeyValue;
            NavigationOptions navigationSelection;
            do
            {
                // KeyChar returns the pure value.
                int.TryParse(Console.ReadKey(true).KeyChar.ToString(), out pressedKeyValue);
                Trace.WriteLine(pressedKeyValue);

                // https://docs.microsoft.com/de-de/dotnet/api/system.enum.tryparse?view=netframework-4.8
                Enum.TryParse(pressedKeyValue.ToString(), out navigationSelection);

                Trace.WriteLine(Enum.IsDefined(typeof(NavigationOptions), navigationSelection));
            } while (!Enum.IsDefined(typeof(NavigationOptions), navigationSelection));

            Trace.WriteLine(navigationSelection);
            switch (navigationSelection)
            {
                // Input grade.
                case NavigationOptions.ADD_GRADE:
                    AddGrade(pStudent);
                    break;
                // Output grade(s).
                case NavigationOptions.SHOW_GRADE:
                    ShowGrade(pStudent);
                    break;
                // Input appointment.
                case NavigationOptions.ADD_APPOINTMENT:
                    AddAppointment(pStudent);
                    break;
                // Output appointment(s).
                case NavigationOptions.SHOW_APPOINTMENT:
                    ShowAppointment(pStudent);
                    break;
                // Calculate certificate Grade.
                case NavigationOptions.CALCULATE_FINAL_GRADE:
                    CalculateCertificateGrade(pStudent);
                    break;
            }
        }

        internal enum NavigationOptions
        {
            ADD_GRADE = 1,
            SHOW_GRADE = 2,
            ADD_APPOINTMENT = 3,
            SHOW_APPOINTMENT = 4,
            CALCULATE_FINAL_GRADE = 5,
            LOGOUT = 6,
            QUIT = 7
        }

        internal void ShowAppointment(Student pStudent)
        {
            BeforeNextActionEvent.Invoke();

            DeserializeAppointment(pStudent);
        }

        internal void ShowGrade(Student pStudent)
        {
            BeforeNextActionEvent.Invoke();

            DeserializeGrade(pStudent);
        }

        internal void ShowLogo()
        {
            Console.Clear();
            Console.WriteLine();
            CenterText("    ▄████  ██▀███   ▄▄▄      ▓█████▄ ▓█████  ██▀███ ▓██   ██▓");
            CenterText("   ██▒ ▀█▒▓██ ▒ ██▒▒████▄    ▒██▀ ██▌▓█   ▀ ▓██ ▒ ██▒▒██  ██▒");
            CenterText("  ▒██░▄▄▄░▓██ ░▄█ ▒▒██  ▀█▄  ░██   █▌▒███   ▓██ ░▄█ ▒ ▒██ ██░");
            CenterText("  ░▓█  ██▓▒██▀▀█▄  ░██▄▄▄▄██ ░▓█▄   ▌▒▓█  ▄ ▒██▀▀█▄   ░ ▐██▓░");
            CenterText("  ░▒▓███▀▒░██▓ ▒██▒ ▓█   ▓██▒░▒████▓ ░▒████▒░██▓ ▒██▒ ░ ██▒▓░");
            CenterText("   ░▒   ▒ ░ ▒▓ ░▒▓░ ▒▒   ▓▒█░ ▒▒▓  ▒ ░░ ▒░ ░░ ▒▓ ░▒▓░  ██▒▒▒ ");
            CenterText("    ░   ░   ░▒ ░ ▒░  ▒   ▒▒ ░ ░ ▒  ▒  ░ ░  ░  ░▒ ░ ▒░▓██ ░▒░ ");
            CenterText("  ░ ░   ░   ░░   ░   ░   ▒    ░ ░  ░    ░     ░░   ░ ▒ ▒ ░░  ");
            CenterText("        ░    ░           ░  ░   ░       ░  ░   ░     ░ ░     ");
            CenterText("                              ░                      ░ ░     ");
            Console.WriteLine();
        }
    }

    #endregion

    #region IUserStrategy

    /// <summary>
    /// Definition of the User Strategy (behaviour).
    /// </summary>
    public interface IUserStrategy
    {
        void Login(User user);
        void Logout();
        void SelectMenuItem();
    }

    #endregion

    #region LoginView

    /// <summary>
    /// View for the Login process.
    /// </summary>
    internal class LoginView : View
    {
        internal override void LoadContext(User user)
        {
            GraderyController.GetInstance().ShowLogo();
            Console.WriteLine("Hallo, zur Notenverwaltung bitte einloggen (Gastzugang: Benutzername: gast, Passwort: 1234:");
            Console.WriteLine("Benutzername: ");
            string userInputUsername = GetUsername();
            Console.WriteLine("Passwort: ");
            string userInputPassword = GetPassword();
            if (LoginController.GetInstance().IsValidUser(userInputUsername, userInputPassword))
            {
                var student = new Student();
                student.GetView(userInputUsername, userInputPassword);
            }
            else
                LoadContext(user);
        }

        private string GetUsername()
        {
            return Console.ReadLine();
        }

        private string GetPassword()
        {
            // copy & paste von: https://stackoverflow.com/questions/3404421/password-masking-console-application
            string pass = "";
            do
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    pass += key.KeyChar;
                    Console.Write("*");
                }
                else if (key.Key == ConsoleKey.Enter)
                    break;
            } while (true);
            return pass;
        }
    }

    #endregion

    #region LoginController

    /// <summary>
    /// Singleton that handles the login process.
    /// </summary>
    internal sealed class LoginController
    {
        // Einzige Instanz der Klasse deklarieren
        private static LoginController Instance;
        // Objekt im aktuellen Thread sperren
        private static readonly object LockThis = new object();

        private LoginController() { }

        internal static LoginController GetInstance()
        {
            // Die lock-Anweisung ruft die Sperre für gegenseitigen Ausschluss für ein bestimmtes Objekt ab,
            // https://docs.microsoft.com/de-de/dotnet/csharp/language-reference/keywords/lock-statement
            lock (LockThis)
                // führt einen Anweisungsblock aus...
                if (Instance == null)
                    // und hebt die Sperre anschließend auf
                    Instance = new LoginController();
            return Instance;
        }

        internal bool IsValidUser(string pUsername, string pPassword)
        {
            return pUsername == "gast" && pPassword == "1234";
        }
    }

    #endregion

    #region SchoolSubject

    /// <summary>
    /// Predefined school subjects.
    /// </summary>
    [Serializable()]
    public enum SchoolSubject
    {
        undefined, AWE, DEU, ENG, ITS, PG, SG, WPG
    }

    #endregion

    #region SerializationController

    /// <summary>
    /// Class responsible for the serialization process.
    /// </summary>
    internal sealed class SerializationController
    {
        private const string PREFIX_APPOINTMENT_DATA = "appointment";
        private const string PREFIX_GRADES_DATA = "grades";
        private const string PREFIX_USER_DATA = "user";

        internal void SerializeAppointment(Appointment appointment)
        {
            SerializeAppointmentToIcs(appointment);
        }

        internal void SerializeAppointmentToIcs(Appointment appointment)
        {
            try
            {
                var fs = new FileStream(appointment.Name + ".ics", FileMode.Append, FileAccess.Write);
                using (var sw = new StreamWriter(fs, Encoding.UTF8))
                    sw.WriteLine(appointment.GetSerializeString());
            }
            catch (IOException e)
            {
                Console.WriteLine(string.Format("An exception occurred:\nError code: {0}\nMessage: {1}",
                    e.HResult & 0x0000FFFF, e.Message));
            }
        }

        internal Appointment DeSerializeAppointmentFromIcs(Student pStudent)
        {
            // Required values for a parsable Appointment.
            string parsedAppointmentName = "undefined";
            DateTime parsedAppointmentDate = DateTime.Now;
            DateTime parsedAppointmentEndDate = DateTime.Now;

            List<string> icsProperties = new List<string>();
            try
            {
                var fs = new FileStream(pStudent.Username + "_" + PREFIX_APPOINTMENT_DATA + ".ics", FileMode.Open, FileAccess.Read);
                using (var sr = new StreamReader(fs, Encoding.UTF8))
                {
                    while (sr.Peek() != -1)
                        icsProperties.Add(sr.ReadLine());
                    Trace.WriteLine(string.Format("Loaded {0} icsProperties.", icsProperties.Count));

                    // Check minimum requirements.
                    if (!(icsProperties.Contains("BEGIN:VCALENDAR") && icsProperties.Contains("END:VCALENDAR")))
                    {
                        Console.WriteLine("Not a valid ics-file.");
                        return null;
                    }

                    // Process summary, start- and (optional) end-date.
                    foreach (var icsProperty in icsProperties)
                    {
                        Trace.WriteLine(string.Format("icsProperty: {0}", icsProperty));
                        if (icsProperty.StartsWith("DTSTART"))
                        {
                            var dateString = icsProperty.Substring(icsProperty.IndexOf(":") + 1);
                            Trace.WriteLine(string.Format("dateString: {0}", dateString));

                            // https://docs.microsoft.com/en-us/dotnet/api/system.datetime.tryparseexact?view=netframework-4.7.2
                            if (DateTime.TryParseExact(dateString, "yyyyMMddTHHmmssZ", CultureInfo.InvariantCulture,
                                     DateTimeStyles.None, out parsedAppointmentDate))
                                Trace.WriteLine(string.Format("Converted '{0}' to {1} ({2}).",
                                    dateString, parsedAppointmentDate, parsedAppointmentDate.Kind));
                            else
                                Trace.WriteLine(string.Format("{0} is not in an acceptable format.", dateString));
                        }
                        else if (icsProperty.StartsWith("SUMMARY"))
                            parsedAppointmentName = icsProperty.Substring(icsProperty.IndexOf(":") + 1);
                        else if (icsProperty.StartsWith("DTEND"))
                        {
                            var dateString = icsProperty.Substring(icsProperty.IndexOf(":") + 1);
                            Trace.WriteLine(string.Format("dateString: {0}", dateString));

                            if (DateTime.TryParseExact(dateString, "yyyyMMddTHHmmssZ", CultureInfo.InvariantCulture,
                                     DateTimeStyles.None, out parsedAppointmentEndDate))
                                Trace.WriteLine(string.Format("Converted '{0}' to {1} ({2}).",
                                    dateString, parsedAppointmentEndDate, parsedAppointmentEndDate.Kind));
                            else
                                Trace.WriteLine(string.Format("{0} is not in an acceptable format.", dateString));
                        }
                    }
                    Trace.WriteLine(string.Format("SUMMARY={0}\nDTSTART={1}\nDTEND={2}",
                        parsedAppointmentName, parsedAppointmentDate, parsedAppointmentEndDate));

                    return new Appointment(parsedAppointmentName, parsedAppointmentDate, parsedAppointmentEndDate);
                }
            }
            catch (IOException e)
            {
                Console.WriteLine(string.Format("An exception occurred:\nError code: {0}\nMessage: {1}",
                    e.HResult & 0x0000FFFF, e.Message));
            }
            return null;
        }

        // https://stackoverflow.com/questions/27025435/how-do-i-read-and-write-a-c-sharp-string-dictionary-to-a-file
        internal void JavaScriptSerializeUserDataToTXT(IDictionary<string, string> d)
        {
            try
            {
                File.WriteAllText(PREFIX_USER_DATA + ".txt", new JavaScriptSerializer().Serialize(d));
            }
            catch (IOException e)
            {
                Console.WriteLine(string.Format("An exception occurred:\nError code: {0}\nMessage: {1}",
                    e.HResult & 0x0000FFFF, e.Message));
            }
        }

        internal IDictionary<string, string> JavaScriptDeserializeUserDataFromTXT()
        {
            try
            {
                return new JavaScriptSerializer().Deserialize<Dictionary<string, string>>(File.ReadAllText(PREFIX_USER_DATA + ".txt"));
            }
            catch (IOException e)
            {
                Console.WriteLine(string.Format("An exception occurred:\nError code: {0}\nMessage: {1}",
                    e.HResult & 0x0000FFFF, e.Message));
            }
            return null;
        }

        internal void SerializeUserDataToJSON(IDictionary<string, string> d)
        {
            try
            {
                File.WriteAllText(PREFIX_USER_DATA + ".json", JsonConvert.SerializeObject(d));
            }
            catch (IOException e)
            {
                Console.WriteLine(string.Format("An exception occurred:\nError code: {0}\nMessage: {1}",
                    e.HResult & 0x0000FFFF, e.Message));
            }
        }

        internal IDictionary<string, string> DeserializeUserDataFromJSON()
        {
            try
            {
                return JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(PREFIX_USER_DATA + ".json"));
            }
            catch (IOException e)
            {
                Console.WriteLine(string.Format("An exception occurred:\nError code: {0}\nMessage: {1}",
                    e.HResult & 0x0000FFFF, e.Message));
            }
            return null;
        }

        internal void SerializeUserDataToXML(IDictionary<string, string> d)
        {
            try
            {
                new XElement("root", d.Select(kv => new XElement(kv.Key, kv.Value)))
                            .Save(PREFIX_USER_DATA + ".xml", SaveOptions.OmitDuplicateNamespaces);
            }
            catch (IOException e)
            {
                Console.WriteLine(string.Format("An exception occurred:\nError code: {0}\nMessage: {1}",
                    e.HResult & 0x0000FFFF, e.Message));
            }
        }

        internal IDictionary<string, string> DeserializeUserDataFromXML()
        {
            try
            {
                return XElement.Parse(File.ReadAllText(PREFIX_USER_DATA + ".xml"))
                               .Elements()
                               .ToDictionary(k => k.Name.ToString(), v => v.Value.ToString());
            }
            catch (IOException e)
            {
                Console.WriteLine(string.Format("An exception occurred:\nError code: {0}\nMessage: {1}",
                    e.HResult & 0x0000FFFF, e.Message));
            }
            return null;
        }

        internal Dictionary<string, string> GetDictionaryExample()
        {
            return new Dictionary<string, string>
            {
                { "hans", "1234" },
                { "dieter", "5678" },
                { "günter", "9012" },
                { "sandra", "3456" }
            };
        }

        internal void SerializeGrade(Student pStudent, Grade pGrade)
        {
            JavaScriptSerializeUserDataToTXT(GetDictionaryExample());
            SerializeUserDataToJSON(GetDictionaryExample());
            SerializeUserDataToXML(GetDictionaryExample());
            SerializeGradeToBinary(pStudent, pGrade);
            SerializeGradeToJson(pStudent, pGrade);
            // Encrypt Grade(s) to ".txt" with a key of 3.
            SerializeGradeToTxt(pStudent, pGrade, 3);
            SerializeGradeToXml(pStudent, pGrade);
        }

        internal void SerializeGradeToBinary(Student pStudent, Grade pGrade)
        {
            var bf = new BinaryFormatter();
            try
            {
                var fs = new FileStream(pStudent.Username + "_" + PREFIX_GRADES_DATA + ".bin", FileMode.Append, FileAccess.Write);
                using (var sw = new StreamWriter(fs, Encoding.UTF8))
                {
                    bf.Serialize(fs, pGrade);
                    sw.Write("\n");

                }
            }
            catch (IOException e)
            {
                Console.WriteLine(string.Format("An exception occurred:\nError code: {0}\nMessage: {1}",
                    e.HResult & 0x0000FFFF, e.Message));
            }
        }

        internal Grade DeSerializeGradeFromBinary(Student pStudent)
        {
            var bf = new BinaryFormatter();
            try
            {
                using (var fs = new FileStream(pStudent.Username + "_" + PREFIX_GRADES_DATA + ".bin", FileMode.Open, FileAccess.Read))
                    return (Grade)bf.Deserialize(fs);
            }
            catch (IOException e)
            {
                Console.WriteLine(string.Format("An exception occurred:\nError code: {0}\nMessage: {1}",
                    e.HResult & 0x0000FFFF, e.Message));
            }
            return null;
        }

        internal void SerializeGradeToJson(Student pStudent, Grade pGrade)
        {
            var js = new JsonSerializer();
            try
            {
                var fs = new FileStream(pStudent.Username + "_" + PREFIX_GRADES_DATA + ".json", FileMode.Append, FileAccess.Write);
                using (var sw = new StreamWriter(fs, Encoding.UTF8))
                using (var jtw = new JsonTextWriter(sw))
                {
                    js.Serialize(jtw, pGrade);
                    sw.Write("\n");
                }
            }
            catch (IOException e)
            {
                Console.WriteLine(string.Format("An exception occurred:\nError code: {0}\nMessage: {1}",
                    e.HResult & 0x0000FFFF, e.Message));
            }
        }

        internal Grade DeSerializeGradeFromJson(Student pStudent)
        {
            var js = new JsonSerializer();
            try
            {
                var fs = new FileStream(pStudent.Username + "_" + PREFIX_GRADES_DATA + ".json", FileMode.Open, FileAccess.Read);
                var sr = new StreamReader(fs, Encoding.UTF8);
                using (var jtr = new JsonTextReader(sr))
                    return (Grade)js.Deserialize(jtr, typeof(Grade));
            }
            catch (IOException e)
            {
                Console.WriteLine(string.Format("An exception occurred:\nError code: {0}\nMessage: {1}",
                    e.HResult & 0x0000FFFF, e.Message));
            }
            return null;
        }

        internal void SerializeGradeToTxt(Student pStudent, Grade pGrade, int pKey)
        {
            try
            {
                var fs = new FileStream(pStudent.Username + "_" + PREFIX_GRADES_DATA + ".txt", FileMode.Append, FileAccess.Write);
                using (var sw = new StreamWriter(fs, Encoding.UTF8))
                {
                    if (pKey > 0)
                        sw.WriteLine(pGrade.GetEncryptedSerializeString(pKey));
                    else
                        sw.WriteLine(pGrade.GetSerializeString());
                }
            }
            catch (IOException e)
            {
                Console.WriteLine(string.Format("An exception occurred:\nError code: {0}\nMessage: {1}",
                    e.HResult & 0x0000FFFF, e.Message));
            }
        }

        internal Grade DeSerializeGradeFromTxt(Student pStudent, int pKey)
        {
            // Required values for a parsable Grade.
            int parsedGradeValue = 0;
            string parsedAppointmentName = "undefined";
            DateTime parsedDate = DateTime.Now;
            SchoolSubject parsedSchoolSubject = SchoolSubject.undefined;

            try
            {
                var fs = new FileStream(pStudent.Username + "_" + PREFIX_GRADES_DATA + ".txt", FileMode.Open, FileAccess.Read);
                using (var sr = new StreamReader(fs, Encoding.UTF8))
                {
                    var grades = new List<string>();
                    while (sr.Peek() != -1)
                    {
                        if (pKey > 0)
                            grades.Add(sr.ReadLine().Decrypt(3));
                        else
                            grades.Add(sr.ReadLine());
                    }
                    Trace.WriteLine(string.Format("Loaded {0} grades.", grades.Count));

                    char[] separator = { ';', '#' };
                    foreach (var grade in grades)
                    {
                        var gradeTokens = grade.Split(separator);
                        foreach (var gradeToken in gradeTokens)
                        {
                            if (gradeToken.Contains("Schulfach"))
                            {
                                Enum.TryParse(gradeToken.Substring(gradeToken.IndexOf("=") + 1), out parsedSchoolSubject);
                                if (!Enum.IsDefined(typeof(SchoolSubject), parsedSchoolSubject))
                                    parsedSchoolSubject = SchoolSubject.undefined;
                            }
                            else if (gradeToken.Contains("Datum"))
                                DateTime.TryParse(gradeToken.Substring(gradeToken.IndexOf("=") + 1), out parsedDate);
                            else if (gradeToken.Contains("Bezeichnung"))
                                parsedAppointmentName = gradeToken.Substring(gradeToken.IndexOf("=") + 1);
                            else if (gradeToken.Contains("Notenwert"))
                                int.TryParse(gradeToken.Substring(gradeToken.IndexOf("=") + 1), out parsedGradeValue);
                        }
                    }
                }
                return new Grade(parsedDate, parsedSchoolSubject, parsedAppointmentName, parsedGradeValue);
            }
            catch (IOException e)
            {
                Console.WriteLine(string.Format("An exception occurred:\nError code: {0}\nMessage: {1}",
                    e.HResult & 0x0000FFFF, e.Message));
            }
            return null;
        }

        // https://stackoverflow.com/questions/1772004/how-can-i-make-the-xmlserializer-only-serialize-plain-xml
        internal void SerializeGradeToXml(Student pStudent, Grade pGrade)
        {
            var serializer = new XmlSerializer(typeof(Grade));
            var settings = new XmlWriterSettings
            {
                Indent = true,
                OmitXmlDeclaration = true
            };
            var emptyNamespaces = new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty });
            try
            {
                // BUG: Error code: 32
                // Message: Der Prozess kann nicht auf die Datei "U:\repos\hhek\Gradery\bin\Release\gast_grades.xml" zugreifen,
                // da sie von einem anderen Prozess verwendet wird.
                // HOW_TO_REPRODUCE: Save Grade, Read Grade (blocks), Save Grade
                var fs = new FileStream(pStudent.Username + "_" + PREFIX_GRADES_DATA + ".xml", FileMode.Append, FileAccess.Write);
                using (var sw = new StreamWriter(fs, Encoding.UTF8))
                using (var xw = XmlWriter.Create(fs, settings))
                {
                    serializer.Serialize(xw, pGrade, emptyNamespaces);
                    sw.Write("\n");
                }
            }
            catch (IOException e)
            {
                Console.WriteLine(string.Format("An exception occurred:\nError code: {0}\nMessage: {1}",
                    e.HResult & 0x0000FFFF, e.Message));
            }
        }

        // https://stackoverflow.com/questions/5042902/xml-error-there-are-multiple-root-elements
        internal Grade DeSerializeGradeFromXml(Student pStudent)
        {
            var serializer = new XmlSerializer(typeof(Grade));
            var settings = new XmlReaderSettings
            {
                ConformanceLevel = ConformanceLevel.Fragment
            };
            try
            {
                var fs = new FileStream(pStudent.Username + "_" + PREFIX_GRADES_DATA + ".xml", FileMode.Open, FileAccess.Read);
                using (XmlReader reader = XmlReader.Create(fs, settings))
                    return (Grade)serializer.Deserialize(reader);
            }
            catch (IOException e)
            {
                Console.WriteLine(string.Format("An exception occurred:\nError code: {0}\nMessage: {1}",
                    e.HResult & 0x0000FFFF, e.Message));
            }
            return null;
        }

        internal void SerializeUserToBinary(Student pUser)
        {
            var bf = new BinaryFormatter();
            try
            {
                var fs = new FileStream(pUser.Username + "_" + PREFIX_GRADES_DATA + ".bin", FileMode.Append, FileAccess.Write);
                using (var sw = new StreamWriter(fs, Encoding.UTF8))
                {
                    bf.Serialize(fs, pUser);
                    sw.Write("\n");
                }
            }
            catch (IOException e)
            {
                Console.WriteLine(string.Format("An exception occurred:\nError code: {0}\nMessage: {1}",
                    e.HResult & 0x0000FFFF, e.Message));
            }
        }

        internal Student DeSerializeUserFromBinary(Student pUser)
        {
            var bf = new BinaryFormatter();
            try
            {
                using (var fs = new FileStream(pUser.Username + "_" + PREFIX_GRADES_DATA + ".bin", FileMode.Open, FileAccess.Read))
                    return (Student)bf.Deserialize(fs);
            }
            catch (IOException e)
            {
                Console.WriteLine(string.Format("An exception occurred:\nError code: {0}\nMessage: {1}",
                    e.HResult & 0x0000FFFF, e.Message));
            }
            return null;
        }
    }

    #endregion

    #region Student

    /// <summary>
    /// Blueprint for Student objects.
    /// </summary>
    internal class Student : User
    {
        internal Student()
        {
            UserStrategy = new StudentStrategy();
        }

        internal override void GetView(string pUsername, string pPassword)
        {
            Username = pUsername;
            Password = pPassword;
            UserStrategy.Login(this);
        }

        internal Appointment GetAppointmentFromUserInput()
        {
            DateTime userInputDate;

            Console.WriteLine("Wie ist der Name des Termins?");
            var userInputDateName = Console.ReadLine();

            Console.WriteLine("Wann ist der Termin? (Eingabeformat: dd/mm/yy)");
            if (!DateTime.TryParse(Console.ReadLine(), out userInputDate))
                return null;

            return new Appointment(userInputDateName, userInputDate);
        }

        internal Grade GetSubGradeFromUserInput()
        {
            float userInputGradeValue;
            DateTime userInputDate;
            SchoolSubject userInputSchoolSubject;

            Console.WriteLine("Wie ist der Name des Schulfaches?");
            Console.WriteLine("(1 = AWE, 2 = DK, 3 = ENG, 4 = ITS, 5 = PG, 6 = SG, 7 = WPG)");
            if (!(Enum.TryParse(Console.ReadLine(), out userInputSchoolSubject)
                && Enum.IsDefined(typeof(SchoolSubject), userInputSchoolSubject)))
                return null;

            Console.WriteLine("Wann war die Leistungsbewertung? (Format: dd/mm/yy)");
            if (!DateTime.TryParse(Console.ReadLine(), out userInputDate))
                return null;

            Console.WriteLine("Wie ist der Notentyp der Leistungsbewertung (Klausur/SoMi)?");
            string userInputGradeType = Console.ReadLine();

            Console.WriteLine("Welchen Wert hat die Note (1-6)?");
            if (!float.TryParse(Console.ReadLine(), out userInputGradeValue))
                return null;

            return new Grade(userInputDate, userInputSchoolSubject, userInputGradeType, userInputGradeValue);
        }

        public override string ToString()
        {
            return "Benutername: " + Username + "\n" + "Passwort: " + Password;
        }
    }

    #endregion

    #region StudentStrategy

    /// <summary>
    /// Student behaviour.
    /// </summary>
    /// <remarks>
    /// Class and it's members must be public due to the implementation of a (public) interface.
    /// </remarks>
    public class StudentStrategy : IUserStrategy
    {
        GraderyView graderyView;

        public void Login(User user)
        {
            graderyView = new GraderyView();
            graderyView.LoadContext(user);
        }

        public void Logout()
        {
            throw new NotImplementedException();
        }

        public void SelectMenuItem()
        {
            throw new NotImplementedException();
        }
    }

    #endregion

    #region User

    /// <summary>
    /// User base class.
    /// </summary>
    /// <remarks>
    /// Class and it's members must be public due it's (software design pattern) implementation.
    /// </remarks>
    public class User
    {
        protected IUserStrategy UserStrategy;
        internal string Username { get; set; }
        internal string Password { get; set; }

        internal virtual void GetView(string pUsername, string pPassword)
        {
            Username = pUsername;
            Password = pPassword;
            UserStrategy.Login(this);
        }
    }

    #endregion

    #region View

    /// <summary>
    /// Base class for all views.
    /// </summary>
    internal class View
    {
        internal View()
        {
        }

        internal virtual void LoadContext(User user)
        {
        }
    }

    #endregion
}