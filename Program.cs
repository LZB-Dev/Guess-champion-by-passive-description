namespace Guess_champion_by_passive_description
{
    using Newtonsoft.Json.Linq;
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.Security.Cryptography;
    using System.Text.Json;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    public class Settings
    {
        public string Language { get; set; }
        public bool Clear { get; set; } //clear html tags from descriptions
        public bool Anonimize { get; set; }
    }

    public class Programm
    {
        private CancellationTokenSource? _cts;
        private static readonly HttpClient client = new HttpClient();


        Settings ImportSettings()
        {
            if (!File.Exists("settings.json") || File.ReadAllText("settings.json") == null)
            {
                File.Create("settings.json").Close();
                File.WriteAllText("settings.json", JsonSerializer.Serialize(new Settings() { Language = "en_US" }));
            }
            Settings settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText("settings.json"))!;
            if (settings.Language == null) { settings.Language = "en_US"; }
            return settings;
        }
       
        async Task Menu(string call)
        {
            Settings settings = ImportSettings();
            switch (call.ToLower())
            {
                case "l" or "lang" or "language":
                    settings.Language = Menu_LanguageSelect(settings.Language);
                    break;
                case "c" or "clear":
                    settings.Clear = !settings.Clear;
                    Console.WriteLine($"\nClear - {settings.Clear}");
                    break;
                case "a" or "anonimize":
                    settings.Anonimize = !settings.Anonimize;
                    Console.WriteLine($"\nAnonimize - {settings.Anonimize}");
                    break;
                case "e" or "exit":
                    Environment.Exit(0);
                    break;
                case "m" or "manual":
                    await Menu_Manual(null);
                    break;
                case string s when s.StartsWith("m ") || s.StartsWith("manual "): //call.Substring("m ".Length) also kinda works
                    string rest = call.Split(' ', 2)[1];
                    await Menu_Manual(rest);
                    break;
                case "f" or "full":
                    await Menu_FullChampionList();
                    break;
            }
            File.WriteAllText("settings.json", JsonSerializer.Serialize(settings));
        }
        
        private async Task Menu_Manual(string? rest)
        {
            if (rest == null) { Console.WriteLine("\nEnter champion name: "); }
            string champ = rest ?? Console.ReadLine();
            if (champ != null && champ != "")
            {
                int langCount = champ.Count(c => c == ' ');
                string id = champ.Split(" ")[0];
                string[] langList = new string[langCount];
                Console.WriteLine("\n-----");
                for (int l = 0; l < langCount; l++)
                {
                    langList[l] = champ.Split(' ')[l + 1];
                }
                List<string[]> info = await GetChampionPassive(id, langList);
                for (int i = 0; i < info.Count; i++)
                {
                    Console.WriteLine($"{i + 1}.  Champion:      {info[i][0]}\n    Name:          {info[i][1]}\n    Description:   {info[i][2]}");
                }
                Console.WriteLine("-----\n");
            }
            else
            {
                Console.WriteLine("Invalid input");
            }
        }
        
        string Menu_LanguageSelect(string language)
        {
            string input = "";
            switch (language)
            {
                case "en_US":
                    Console.WriteLine("\nChoose language (l - language list) \nEnglish selected");
                    input = Console.ReadLine()!;
                    break;

                case "ru_RU":
                    Console.WriteLine("\nВыбери язык (l - список языков)  \nРусский выбран");
                    input = Console.ReadLine()!;
                    break;
                default:
                    Console.WriteLine($"\nChoose language (l - language list)  \nSelected language: {language}");
                    input = Console.ReadLine()!;
                    break;
            }
            //cs_cz, el_gr, pl_pl, ro_ro, hu_hu, en_gb, de_de, es_es, it_it, fr_fr, ja_jp, ko_kr, es_mx, es_ar, pt_br, en_us, en_au, ru_ru, tr_tr, ms_my, en_ph, en_sg, th_th, vi_vn, id_id, zh_my, zh_cn, zh_tw, 
            if (input.ToLower() == "l")
            {
                input = LanguageHelp(language)!;
            }
            language = LanguageRecognizor(input);
            Console.WriteLine($"\nSelected language: {language}");
            return language;
        }

        private async Task Menu_FullChampionList()
        {
            Console.WriteLine("\n");
            List<string> list = await CreateChampionList();
            for (int i = 0; i < list.Count; i++)
            {
                string[] info = await GetChampionPassive(list[i]);
                Console.WriteLine($"{i+1}.{info[0]}\n  :{info[2]}\n");
            }
        }

        string LanguageHelp(string language)
        {
            if (language == "ru_RU")
            {
                Console.WriteLine("Пример: en_us (English). Допустимо 'en_us', 'en', 'us', 'English'\nСписок языков:");
            }
            else
            {
                Console.WriteLine("Example: en_us (English). Aceptable 'en_us', 'en', 'us', 'English'\nLanguage List:");
            }
            Console.WriteLine("\ncs_cz (Czech)\nel_gr (Greek)\npl_pl (Polish)\nro_ro (Romanian)\nhu_hu (Hungarian)\nen_gb (English UK)\nde_de (German)\nes_es (Spanish Spain)\nit_it (Italian)\nfr_fr (French)\nja_jp (Japanese)\nko_kr (Korean)\nes_mx (Spanish Mexico)\nes_ar (Spanish Argentina)\npt_br (Portuguese Brazil)\nen_us (English US)\nen_au (English Australia)\nru_ru (Russian)\ntr_tr (Turkish)\nms_my (Malay)\nen_ph (English Philippines)\nen_sg (English Singapore)\nth_th (Thai)\nvi_vn (Vietnamese)\nid_id (Indonesian)\nzh_my (Chinese Malaysia)\nzh_cn (Chinese China)\nzh_tw (Chinese Taiwan)");
            return Console.ReadLine();
        }

        string LanguageRecognizor(string input)
        {
            Settings settings = ImportSettings();

            string language = input.ToLower() switch
            {
                "cs_cz" or "cs" or "cz" or "Czech" => "cs_CZ",
                "el_gr" or "el" or "gr" or "Greek" => "el_GR",
                "pl_pl" or "pl" or "Polish" => "pl_PL",
                "ro_ro" or "ro" or "Romanian" => "ro_RO",
                "hu_hu" or "hu" or "Hungarian" => "hu_HU",
                "en_gb" or "gb" or "English (United Kingdom)" => "en_GB",
                "en_us" or "en" or "us" or "English" => "en_US",
                "en_au" or "English (Australia)" => "en_AU",
                "en_ph" or "ph" or "English (Philippines)" => "en_PH",
                "en_sg" or "sg" or "English (Singapore)" => "en_SG",
                "de_de" or "de" or "German" => "de_DE",
                "es_es" or "es" or "Spanish" => "es_ES",
                "it_it" or "it" or "Italian" => "it_IT",
                "fr_fr" or "fr" or "French" => "fr_FR",
                "ja_jp" or "ja" or "Japanese" => "ja_JP",
                "ko_kr" or "ko" or "kr" or "Korean" => "ko_KR",
                "es_mx" or "es" or "mx" or "Spanish (Mexico)" => "es_MX",
                "es_ar" or "ar" or "Spanish (Argentina)" => "es_AR",
                "pt_br" or "pt" or "br" or "Portuguese" => "pt_BR",
                "ru_ru" or "ru" or "Russian" => "ru_RU",
                "tr_tr" or "tr" or "Turkish" => "tr_TR",
                "ms_my" or "ms" or "my" or "Malay" => "ms_MY",
                "th_th" or "th" or "Thai" => "th_TH",
                "vi_vn" or "vi" or "vn" or "Vietnamese" => "vi_VN",
                "id_id" or "id" or "Indonesian" => "id_ID",
                "zh_my" or "my" or "Chinese (Malaysia)" => "zh_MY",
                "zh_cn" or "zh" or "cn" or "Chinese" => "zh_CN",
                "zh_tw" or "tw" or "Chinese(Taiwan)" => "zh_TW",
                _ => settings.Language,
            };
            return language;
        }


        string ChampionRandomizer(List<string> list)
        {
            Random Random = new Random();
            string champ = list[Random.Next(list.Count)];
            Console.WriteLine(champ);
            return champ;
        }

        async Task<List<string>> CreateChampionList()
        {
            string urlChampList = $"https://ddragon.leagueoflegends.com/cdn/15.18.1/data/en_US/champion.json";

            var json = await client.GetAsync(urlChampList);
            json.EnsureSuccessStatusCode();

            string jsonBody = await json.Content.ReadAsStringAsync();

            JObject Root = JObject.Parse(jsonBody);
            JObject Champs = (JObject)Root["data"]!;

            var ChampList = new List<string>();
            foreach (var champ in Champs.Properties())
            {
                JObject joChamp = (JObject)champ.Value;
                ChampList.Add(joChamp["id"]!.ToString());
            }
            //foreach(string temp in ChampList)
            //{
            //    Console.WriteLine(temp);
            //}
            return ChampList;
        }

        async Task<string[]> GetChampionPassive(string champ)   //champion, passive name and description
        {
            Settings settings = ImportSettings();
            string urlChampionPage = $"https://ddragon.leagueoflegends.com/cdn/15.18.1/data/{settings.Language}/champion/{champ}.json";
            var json = await client.GetAsync(urlChampionPage);
            json.EnsureSuccessStatusCode();
            string jsonBody = await json.Content.ReadAsStringAsync();
            JObject root = JObject.Parse(jsonBody);
            JObject joData = (JObject)root["data"]![$"{champ}"]!;
            string[] info = { joData["name"]!.ToString(), joData["passive"]!["name"]!.ToString(), joData["passive"]!["description"]!.ToString() };
            if (settings.Clear == true) { info[2] = Regex.Replace(info[2], "<.*?>", ""); }
            if (settings.Anonimize == true) { info[2] = AnonimizeDescription(info); }
            return info;
        }

        async Task<List<string[]>> GetChampionPassive(string champ, string[] langList)   //champion, passive name and description
        {
            Settings settings = ImportSettings();
            List<string[]> infoList = new List<string[]>();
            for (int i = 0; i < langList.Length; i++)
            {
                string language = LanguageRecognizor(langList[i]);
                string urlChampionPage = $"https://ddragon.leagueoflegends.com/cdn/15.18.1/data/{language}/champion/{champ}.json";
                var json = await client.GetAsync(urlChampionPage);
                json.EnsureSuccessStatusCode();
                string jsonBody = await json.Content.ReadAsStringAsync();
                JObject root = JObject.Parse(jsonBody);
                JObject joData = (JObject)root["data"]![$"{champ}"]!;
                string[] info = { joData["name"]!.ToString(), joData["passive"]!["name"]!.ToString(), joData["passive"]!["description"]!.ToString() };
                if (settings.Clear == true) { info[2] = Regex.Replace(info[2], "<.*?>", ""); }
                if (settings.Anonimize == true) { info[2] = AnonimizeDescription(info); }
                infoList.Add(info);
            }
            return infoList;
        }

        string AnonimizeDescription(string[] info) //anonimize - replace name with CHAMPION and she/her with he/him
        {
            //English
            string description = info[2];
            description = description.Replace(info[0], "CHAMPION");
            description = Regex.Replace(description, "(?<= )her(?= )", "HIS");
            description = Regex.Replace(description, "(?<= )she(?= )", "HE");
            description = Regex.Replace(description, "(?<= )Her(?= )", "HIS");
            description = Regex.Replace(description, "(?<= )She(?= )", "HE");
            return description;
        }






        public static async Task Main(string[] args)
        {
            Programm p = new Programm();
            Settings s = p.ImportSettings();
            Console.WriteLine($"Menu:\n l/lang/language - Change language (Language - {s.Language})"+
                $"\n c/clear - on/off deleting of html tags (Clear - {s.Clear})\n a/anonimize - hide champion name and gender (Anonimize - {s.Anonimize})"+
                $"\n e/exit - Exit\n\n m/manual - Manual champ select\nf/full - Full description list\n");
            while (true)
            {
                string call = Console.ReadLine(); //input can be null
                if (call != null && call != "")
                {
                    await p.Menu(call);
                    Console.ReadKey();
                }
                List<string> champions = await p.CreateChampionList();
                p.CreateChampionList().Wait();
                string champ = p.ChampionRandomizer(champions);
                //champ = "Rakan"; //for testing
                string[] passive = await p.GetChampionPassive(champ);
                Console.WriteLine($"Champion:      {passive[0]}\nName:          {passive[1]}\nDescription:   {passive[2]}");
            }
        }
    }
}