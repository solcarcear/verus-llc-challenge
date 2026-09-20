using CompanyManagement.Domain;

namespace CompanyManagement.Infrastructure.Persistence.Seed;

// Pure, deterministic generator: Generate(n) always returns the same n companies,
// and Generate(n) is always a prefix of Generate(m) for m > n, because each
// record's data is derived from its own index rather than from shared RNG state.
// This lets tests exercise a small count while Development seeding uses 5000.
//
// Only Name and WebsiteUrl are generated because those are the only fields on
// the Company domain model (CompanyManagement.Domain.Company).
public static class CompanySeedDataGenerator
{
    // ASCII "SEED" (0x53 'S', 0x45 'E', 0x45 'E', 0x44 'D') as the Guid's first
    // component, so every seeded row's Id renders as "53454544-0000-0000-...".
    // That makes seeded rows identifiable/queryable without an extra column or
    // migration, and lets the seeder check for prior seeding via a single,
    // cheap primary-key lookup instead of a table-wide COUNT/Any().
    private const int SeedIdMagic = 0x53454544;

    private static readonly string[] CoreWords =
    {
        "Vertex", "Summit", "Horizon", "Pioneer", "Falcon", "Cedar", "Granite", "Sterling", "Meridian", "Atlas",
        "Beacon", "Crestwood", "Ember", "Fusion", "Ironclad", "Juniper", "Keystone", "Lumen", "Maple", "Nimbus",
        "Orbit", "Pinnacle", "Quantum", "Redwood", "Sable", "Titan", "Union", "Vantage", "Westbrook", "Zenith",
        "Anchor", "Brightside", "Clearview", "Driftwood", "Everline", "Frontier", "Glacier", "Harborview", "Ironwood",
        "Lakeside", "Northgate", "Oakridge", "Palisade", "Riverside", "Stonebridge", "Timberline", "Ashford", "Bellmont",
        "Copperfield", "Dunwood", "Eastbrook", "Fairhaven", "Greystone", "Hillcrest", "Ridgeline", "Southport",
        "Wrenfield", "Amberly", "Birchwood", "Cobalt", "Everest", "Fairwind", "Goldleaf", "Highpoint",
    };

    private static readonly string[] IndustryWords =
    {
        "Technologies", "Solutions", "Systems", "Logistics", "Consulting", "Partners", "Industries", "Manufacturing",
        "Foods", "Robotics", "Analytics", "Networks", "Capital", "Ventures", "Media", "Energy", "Pharmaceuticals",
        "Healthcare", "Software", "Materials", "Construction", "Realty", "Financial", "Insurance", "Apparel",
        "Automotive", "Aerospace", "Biotech", "Chemicals", "Electronics", "Retail", "Shipping", "Telecom", "Security",
        "Publishing", "Design", "Engineering", "Agriculture", "Mining", "Hospitality", "Dynamics", "Innovations",
        "Labs", "Works", "Digital", "Freight", "Textiles", "Instruments", "Resources",
    };

    private static readonly string[] Suffixes = { "Inc", "LLC", "Corp", "Corporation", "Ltd", "Limited", "Group", "Co", "" };

    private static readonly string[] Tlds = { "com", "net", "io", "co", "biz", "org", "tech", "app", "us" };

    // Disjoint from CoreWords/IndustryWords by construction, so a domain built
    // purely from these words can never accidentally share a token with a
    // generated company name.
    private static readonly string[] UnrelatedDomainWords =
    {
        "brightfox", "cloudpeak", "silverleaf", "mosaictrail", "bluejay", "copperfern", "opalridge", "emberlynx",
        "frostgate", "willowmark", "cinderpath", "duskwillow", "palereach", "coppervale", "hazelcourt", "ravenridge",
        "mintfield", "clovertide", "driftmoor", "saffronbay", "thistledown", "ambercreek", "cascadewren", "foxglove",
        "harborlynx", "ironvale", "junipertide", "larkspurrun", "mapledrift", "nightfall", "oceanwren", "pinevale",
    };

    private enum RelevanceCategory
    {
        Exact,
        Containment,
        TokenMatch,
        NotRelevant,
    }

    public static Guid SeedId(int index)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return new Guid(SeedIdMagic, 0, 0, BitConverter.GetBytes((long)index));
    }

    public static IReadOnlyList<Company> Generate(int count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        var companies = new List<Company>(count);
        for (var index = 0; index < count; index++)
        {
            companies.Add(GenerateAt(index));
        }

        return companies;
    }

    private static Company GenerateAt(int index)
    {
        // A fresh, index-seeded RNG (rather than one shared stream walked in
        // order) is what makes Generate(n) a stable prefix of Generate(m).
        var rng = new Random(unchecked(SeedIdMagic + (index * 7919)));

        var (name, meaningfulWords) = BuildName(rng);
        var category = PickRelevanceCategory(rng);
        var host = BuildHost(rng, meaningfulWords, category);
        var tld = Tlds[rng.Next(Tlds.Length)];
        var includeWww = rng.Next(100) < 50;
        var scheme = rng.Next(100) < 80 ? "https" : "http";

        var websiteUrl = $"{scheme}://{(includeWww ? "www." : string.Empty)}{host}.{tld}";

        return new Company(SeedId(index), name, websiteUrl);
    }

    // Returns the generated name together with the "meaningful" words used to
    // build it (i.e. everything except the corporate suffix and the "&"
    // separator) — this always has at least 2 entries, which BuildHost relies
    // on to tell Exact and Containment domains apart.
    private static (string Name, IReadOnlyList<string> MeaningfulWords) BuildName(Random rng)
    {
        var core = CoreWords[rng.Next(CoreWords.Length)];
        var industry = IndustryWords[rng.Next(IndustryWords.Length)];
        var suffix = Suffixes[rng.Next(Suffixes.Length)];
        var useSecondCore = rng.Next(100) < 20;

        var meaningfulWords = new List<string> { core };
        var nameParts = new List<string> { core };

        if (useSecondCore)
        {
            string core2;
            do
            {
                core2 = CoreWords[rng.Next(CoreWords.Length)];
            }
            while (core2 == core);

            meaningfulWords.Add(core2);
            nameParts.Add("&");
            nameParts.Add(core2);
        }

        meaningfulWords.Add(industry);
        nameParts.Add(industry);

        if (!string.IsNullOrEmpty(suffix))
        {
            nameParts.Add(suffix);
        }

        return (string.Join(' ', nameParts), meaningfulWords);
    }

    private static RelevanceCategory PickRelevanceCategory(Random rng)
    {
        var roll = rng.Next(100);
        return roll switch
        {
            < 55 => RelevanceCategory.Exact,
            < 75 => RelevanceCategory.Containment,
            < 90 => RelevanceCategory.TokenMatch,
            _ => RelevanceCategory.NotRelevant,
        };
    }

    // Mirrors CompanyRelevanceEvaluator's token/identity comparison so each
    // category deterministically produces the outcome it's named for:
    // Exact -> ExactMatchScore, Containment -> ContainmentMatchScore,
    // TokenMatch -> TokenMatchScore, NotRelevant -> no match at all.
    //
    // TokenMatch and NotRelevant join their two words with a hyphen rather
    // than concatenating them directly. CompanyRelevanceEvaluator's
    // ExtractDomainTokens only splits a host label on non-alphanumeric
    // characters, so an unseparated "foxglovetextiles" would parse back as a
    // single token and could never share a token with the company name -
    // silently collapsing the TokenMatch category into NotRelevant.
    private static string BuildHost(Random rng, IReadOnlyList<string> meaningfulWords, RelevanceCategory category) =>
        category switch
        {
            RelevanceCategory.Exact => string.Concat(meaningfulWords.Select(w => w.ToLowerInvariant())),
            RelevanceCategory.Containment => meaningfulWords[0].ToLowerInvariant(),
            RelevanceCategory.TokenMatch => string.Join(
                '-',
                UnrelatedDomainWords[rng.Next(UnrelatedDomainWords.Length)],
                meaningfulWords[^1].ToLowerInvariant()),
            _ => string.Join(
                '-',
                UnrelatedDomainWords[rng.Next(UnrelatedDomainWords.Length)],
                UnrelatedDomainWords[rng.Next(UnrelatedDomainWords.Length)]),
        };
}
