namespace DesktopAiTranslator.Translation;

public static class PromptBuilder
{
    public static string BuildSystemPrompt(TranslationRequest request)
    {
        var mode = request.Mode switch
        {
            "fluent" => "翻译风格：自然流畅。",
            "legal" => "翻译风格：法律严谨。",
            "academic" => "翻译风格：学术表达。",
            "bilingual" => "翻译风格：双语输出，保留原文并给出译文。",
            _ => "翻译风格：准确翻译。"
        };

        return
            "你是专业翻译引擎。请将用户提供的文本翻译为目标语言：" + request.TargetLanguage + "。\n\n" +
            "要求：\n" +
            "1. 保留原文含义。\n" +
            "2. 不添加解释。\n" +
            "3. 不遗漏数字、日期、单位、金额、专有名词。\n" +
            "4. 法律、金融、医学、技术内容保持严谨。\n" +
            "5. 如果原文是零散短语，请直接给出自然译文。\n" +
            "6. 如果原文包含列表、编号、条款，请尽量保留结构。\n" +
            "7. 只输出译文，不输出额外说明。\n\n" +
            mode;
    }
}
