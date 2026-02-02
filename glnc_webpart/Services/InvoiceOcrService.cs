using System.Globalization;
using System.Text.RegularExpressions;
using Tesseract;

namespace glnc_webpart.Services
{
    public class InvoiceOcrService : IInvoiceOcrService
    {
        private readonly ILogger<InvoiceOcrService> _logger;
        private readonly IWebHostEnvironment _env;

        private static readonly string[] ExcludedWords =
        {
            "SODEVIA", "FACTURE", "LIVRAISON", "COMMANDE", "COMPTABILITE", "DIRECTION",
            "REPRESENTANT", "TRANSPORTEUR", "CODE CLIENT", "REFERENCE", "CLIENT",
            "NOUMEA", "NOUVELLE", "CALEDONIE", "TOTAL", "PRIX", "MONTANT", "SOMME",
            "FRANCS", "XPF", "ARRETE", "PRESENTE", "REGLEMENT", "ESPECE", "PAGE"
        };

        private static readonly Regex BusinessTermsRegex = new(
            @"SUPERMARKET|SARL|SAS|SOCIETE|MARCHE|GROS|BOULARI|KORAIL|THIRIET|ALIMENTAIRE|OCEANIENNE|NORMANDIE|APOGOTI|BALLANDE",
            RegexOptions.IgnoreCase);

        public InvoiceOcrService(ILogger<InvoiceOcrService> logger, IWebHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        public async Task<InvoiceOcrResult> ExtractFromImageAsync(Stream imageStream, string contentType, CancellationToken cancellationToken = default)
        {
            string? tempPath = null;
            try
            {
                // Save stream to temp file (Tesseract needs file path)
                tempPath = Path.Combine(Path.GetTempPath(), $"invoice_ocr_{Guid.NewGuid():N}.tmp");
                await using (var fs = File.Create(tempPath))
                {
                    await imageStream.CopyToAsync(fs, cancellationToken);
                }

                var tessDataPath = Path.Combine(_env.ContentRootPath, "tessdata");
                if (!Directory.Exists(tessDataPath))
                {
                    Directory.CreateDirectory(tessDataPath);
                }

                var fraPath = Path.Combine(tessDataPath, "fra.traineddata");
                var engPath = Path.Combine(tessDataPath, "eng.traineddata");
                var language = File.Exists(fraPath) ? "fra" : (File.Exists(engPath) ? "eng" : "fra");

                if (!File.Exists(fraPath) && !File.Exists(engPath))
                {
                    _logger.LogWarning("No tessdata found in {Path}. OCR may fail. Download fra.traineddata from https://github.com/tesseract-ocr/tessdata", tessDataPath);
                }

                string text;
                using (var engine = new TesseractEngine(tessDataPath, language, EngineMode.Default))
                {
                    using var pix = Pix.LoadFromFile(tempPath);
                    using var page = engine.Process(pix);
                    text = page.GetText();
                }

                if (string.IsNullOrWhiteSpace(text))
                {
                    return new InvoiceOcrResult(false, null, null, "Aucun texte détecté dans l'image.");
                }

                _logger.LogInformation("OCR extracted {Length} characters", text.Length);

                var clientName = ExtractCustomerName(text);
                var weight = ExtractWeight(text);

                return new InvoiceOcrResult(true, clientName, weight, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OCR extraction failed");
                return new InvoiceOcrResult(false, null, null, $"Erreur lors de l'analyse: {ex.Message}");
            }
            finally
            {
                if (!string.IsNullOrEmpty(tempPath) && File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); } catch { /* ignore */ }
                }
            }
        }

        private static bool IsExcluded(string line)
        {
            var upper = line.ToUpperInvariant();
            return ExcludedWords.Any(ex =>
                upper == ex || Regex.IsMatch(upper, $@"\b{Regex.Escape(ex)}\b"));
        }

        private static string? ExtractCustomerName(string text)
        {
            var lines = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .Where(l => l.Length > 0)
                .ToArray();

            // Strategy 0: "Facture FA0540506 KORAIL NORMANDIE SUPERMARKET BARQUET"
            var factureMatch = Regex.Match(text, @"facture\s+[A-Z0-9]+\s+([A-ZÀÂÄÇÉÈÊËÏÎÔÖÙÛÜÆŒ\s'-]{8,50})", RegexOptions.IgnoreCase);
            if (factureMatch.Success)
            {
                var name = factureMatch.Groups[1].Value.Trim();
                if (!IsExcluded(name) && name.Length >= 8 && (BusinessTermsRegex.IsMatch(name) || name.Length >= 12))
                    return name;
            }

            // Strategy 1: Lines before address
            for (var i = 0; i < lines.Length; i++)
            {
                if (Regex.IsMatch(lines[i], @"\d{5}.*(?:NOUMEA|MONT|DUMBEA|NORMANDIE|BP|B\.P\.|AVENUE|RUE|LOT|SECTION|VILLA|RESIDENCE)", RegexOptions.IgnoreCase) ||
                    Regex.IsMatch(lines[i], @"(?:BP|B\.P\.)\s*\d+", RegexOptions.IgnoreCase))
                {
                    for (var j = Math.Max(0, i - 5); j < i; j++)
                    {
                        var candidate = lines[j];
                        if (Regex.IsMatch(candidate, @"^[A-ZÀÂÄÇÉÈÊËÏÎÔÖÙÛÜÆŒ\s'-]{8,50}$") &&
                            !IsExcluded(candidate) && !Regex.IsMatch(candidate, @"^\d") &&
                            (BusinessTermsRegex.IsMatch(candidate) || candidate.Length >= 12))
                            return candidate;
                    }
                }
            }

            // Strategy 2: CODE + NAME
            foreach (var line in lines)
            {
                var match = Regex.Match(line, @"^\d{3,6}\s+([A-ZÀÂÄÇÉÈÊËÏÎÔÖÙÛÜÆŒ\s'-]{8,50})$");
                if (match.Success)
                {
                    var name = match.Groups[1].Value.Trim();
                    if (!Regex.IsMatch(name, @"^\d+$") && name.Length >= 8 && !IsExcluded(name))
                        return name;
                }
            }

            // Strategy 4: Fallback - uppercase with business terms
            var candidates = new List<string>();
            foreach (var line in lines)
            {
                if (Regex.IsMatch(line, @"^[A-ZÀÂÄÇÉÈÊËÏÎÔÖÙÛÜÆŒ\s'-]{8,50}$") &&
                    !IsExcluded(line) && !Regex.IsMatch(line, @"^\d") &&
                    !Regex.IsMatch(line, @"^(TOTAL|PRIX|MONTANT|SOMME|FRANCS|XPF|ARRETE|PRESENTE)", RegexOptions.IgnoreCase))
                {
                    if (BusinessTermsRegex.IsMatch(line))
                        return line;
                    candidates.Add(line);
                }
            }
            return candidates.OrderByDescending(c => c.Length).FirstOrDefault();
        }

        private static double? ExtractWeight(string text)
        {
            var lines = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .Where(l => l.Length > 0)
                .ToArray();

            // Strategy 1: "Poids Livré" - line below
            for (var i = 0; i < lines.Length; i++)
            {
                if (!Regex.IsMatch(lines[i], @"poids\s+livr", RegexOptions.IgnoreCase)) continue;

                if (i + 1 < lines.Length)
                {
                    var lineBelow = lines[i + 1];
                    var decimalMatch = Regex.Match(lineBelow, @"\b(\d{2,5}\s*[,\.]\s*\d{1,3})\b");
                    if (decimalMatch.Success)
                    {
                        var numStr = decimalMatch.Value.Replace(" ", "").Replace(",", ".");
                        if (double.TryParse(numStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var num) &&
                            num >= 0.1 && num <= 10000)
                            return Math.Round(num, 2);
                    }
                    var intMatch = Regex.Matches(lineBelow, @"\b(\d{1,6})\b")
                        .Select(m => int.TryParse(m.Value, out var v) ? v : 0)
                        .Where(v => v >= 1 && v <= 10000 && !Regex.IsMatch(v.ToString(), @"^(19|20)\d{2}$"))
                        .OrderByDescending(v => v)
                        .FirstOrDefault();
                    if (intMatch > 0)
                        return Math.Round(intMatch >= 10000 ? intMatch / 1000.0 : intMatch, 2);
                }

                var sameLine = Regex.Match(lines[i], @"poids\s+livr[ée][\s|:]*(\d{2,5}[,\.]\d{1,3})", RegexOptions.IgnoreCase);
                if (sameLine.Success)
                {
                    var numStr = sameLine.Groups[1].Value.Replace(",", ".");
                    if (double.TryParse(numStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var num) &&
                        num >= 0.1 && num <= 10000)
                        return Math.Round(num, 2);
                }
            }

            // Strategy 2: "Poids" without Livré
            foreach (var line in lines)
            {
                if (Regex.IsMatch(line, @"^poids\s*:?", RegexOptions.IgnoreCase) && !Regex.IsMatch(line, @"livr", RegexOptions.IgnoreCase))
                {
                    var m = Regex.Match(line, @"poids\s*:?\s*(\d+[,\.]\d{1,3})", RegexOptions.IgnoreCase);
                    if (m.Success)
                    {
                        var numStr = m.Groups[1].Value.Replace(",", ".");
                        if (double.TryParse(numStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var num) &&
                            num >= 0.1 && num <= 10000)
                            return Math.Round(num, 2);
                    }
                }
            }

            // Strategy 3: Quantité
            var quantiteMatches = Regex.Matches(text, @"quantit[ée]\s*:?\s*(\d+[,\.]\d{1,3})", RegexOptions.IgnoreCase);
            if (quantiteMatches.Count > 0)
            {
                var last = quantiteMatches[^1].Groups[1].Value.Replace(",", ".");
                if (double.TryParse(last, NumberStyles.Any, CultureInfo.InvariantCulture, out var num) &&
                    num >= 0.1 && num <= 10000)
                    return Math.Round(num, 2);
            }

            return null;
        }
    }
}
