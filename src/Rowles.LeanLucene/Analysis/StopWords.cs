namespace Rowles.LeanLucene.Analysis;

/// <summary>
/// Stop word lists for common languages.
/// </summary>
public static class StopWords
{
    public static readonly IReadOnlyList<string> English = StopWordFilter.DefaultStopWords;

    public static readonly IReadOnlyList<string> French =
    [
        "au", "aux", "avec", "ce", "ces", "dans", "de", "des", "du",
        "elle", "en", "est", "et", "eux", "il", "je", "la", "le", "les",
        "leur", "lui", "ma", "mais", "me", "même", "mes", "mon", "ne",
        "nos", "notre", "nous", "on", "ou", "par", "pas", "pour",
        "qu", "que", "qui", "sa", "se", "ses", "son", "sont", "sur", "ta",
        "te", "tes", "toi", "ton", "tu", "un", "une", "vos", "votre",
        "vous", "c", "d", "j", "l", "m", "n", "s", "t", "y"
    ];

    public static readonly IReadOnlyList<string> German =
    [
        "aber", "alle", "allem", "allen", "aller", "allerdings", "als",
        "also", "am", "an", "andere", "anderem", "anderen", "anderer",
        "anderes", "anderm", "andern", "anderr", "anders", "auch", "auf",
        "aus", "bei", "beim", "bin", "bis", "bist", "da", "damit",
        "dann", "das", "dass", "dem", "den", "denn", "der", "des",
        "die", "dies", "diese", "diesem", "diesen", "dieser", "dieses",
        "doch", "dort", "du", "durch", "ein", "eine", "einem", "einen",
        "einer", "einige", "einigem", "einigen", "einiger", "einiges",
        "er", "es", "etwas", "euch", "euer", "eure", "eurem",
        "euren", "eurer", "für", "hat", "hatte", "hier", "hin",
        "hinter", "ich", "ihm", "ihn", "ihnen", "ihr", "ihre",
        "ihrem", "ihren", "ihrer", "im", "in", "ist", "jede",
        "jedem", "jeden", "jeder", "jedes", "jene", "jenem", "jenen",
        "jener", "jenes", "kann", "kein", "keine", "keinem", "keinen",
        "keiner", "man", "mein", "meine", "meinem", "meinen",
        "meiner", "mit", "muss", "nach", "nicht", "noch", "nun",
        "nur", "ob", "oder", "ohne", "sehr", "sein", "seine",
        "seinem", "seinen", "seiner", "sich", "sie", "sind", "so",
        "soll", "und", "uns", "unser", "unsere", "unserem", "unseren",
        "unserer", "über", "um", "von", "vor", "war", "was",
        "weil", "welch", "welche", "welchem", "welchen", "welcher",
        "welches", "wenn", "wer", "wie", "wieder", "will", "wir",
        "wird", "wo", "zu", "zum", "zur"
    ];

    public static readonly IReadOnlyList<string> Russian =
    [
        "а", "без", "более", "бы", "был", "была", "были", "было",
        "быть", "в", "вам", "вас", "весь", "во", "вот", "все",
        "всего", "всех", "вы", "где", "да", "даже", "для", "до",
        "его", "ее", "если", "есть", "еще", "же", "за", "здесь",
        "и", "из", "или", "им", "их", "к", "как", "ко", "когда",
        "кто", "ли", "либо", "мне", "может", "мой", "моя", "мы",
        "на", "надо", "наш", "не", "него", "нее", "нет", "ни",
        "них", "но", "ну", "о", "об", "однако", "он", "она",
        "они", "оно", "от", "по", "под", "при", "с", "со",
        "так", "также", "такой", "там", "те", "тем", "то",
        "того", "тоже", "той", "только", "том", "ты", "у",
        "уже", "хотя", "чего", "чей", "чем", "что", "чтобы",
        "чье", "чья", "эта", "эти", "это", "я"
    ];

    public static readonly IReadOnlyList<string> Chinese =
    [
        "的", "了", "在", "是", "我", "有", "和", "就", "不", "人",
        "都", "一", "一个", "上", "也", "很", "到", "说", "要", "去",
        "你", "会", "着", "没有", "看", "好", "自己", "这"
    ];

    /// <summary>Gets the stop word list for a language code, or null if not available.</summary>
    public static IReadOnlyList<string>? ForLanguage(string languageCode)
        => languageCode.ToLowerInvariant() switch
        {
            "en" => English,
            "fr" => French,
            "de" => German,
            "ru" => Russian,
            "zh" => Chinese,
            _ => null
        };
}
