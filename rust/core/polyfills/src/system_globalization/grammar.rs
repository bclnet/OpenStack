// PORT-SOURCE: Core/OpenStack.Polyfills/System.Globalization/Grammar.cs
// PORT-SHA: ca995a91c9d4f0d9
// PORT-STATUS: done
//
// English pluralisation and ordinals for generated game text.

/// C# `StartsWithVowel(this string)`.
pub fn starts_with_vowel(s: &str) -> bool {
    s.chars()
        .next()
        .map(|c| matches!(c.to_ascii_lowercase(), 'a' | 'e' | 'i' | 'o' | 'u'))
        .unwrap_or(false)
}

/// C# `Pluralize(this string)`.
///
/// The `us -> uss` rule is deliberate on the C# side, with a comment citing a
/// packet capture: the game itself emits "Sarcophaguss". Preserved, since the
/// point is matching the server's text, not correct Latin.
pub fn pluralize(name: &str) -> String {
    let ends = |suf: &str| name.ends_with(suf);
    if ends("us") {
        format!("{name}s")
    } else if ends("ch") || ends("s") || ends("sh") || ends("x") || ends("z") {
        format!("{name}es")
    } else if ends("th") {
        name.to_string()
    } else {
        format!("{name}s")
    }
}

/// C# `ToOrdinalSuffix(this int)`.
///
/// Returns `None` for negatives; the C# threw `ArgumentOutOfRangeException`.
pub fn ordinal_suffix(n: i32) -> Option<&'static str> {
    if n < 0 {
        return None;
    }
    Some(match n % 100 {
        11 | 12 | 13 => "th",
        _ => match n % 10 {
            1 => "st",
            2 => "nd",
            3 => "rd",
            _ => "th",
        },
    })
}

/// C# `ToOrdinal(this int)`.
pub fn ordinal(n: i32) -> Option<String> {
    ordinal_suffix(n).map(|s| format!("{n}{s}"))
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn vowel_detection_is_case_insensitive() {
        assert!(starts_with_vowel("Apple"));
        assert!(starts_with_vowel("egg"));
        assert!(!starts_with_vowel("dog"));
        assert!(!starts_with_vowel(""));
    }

    #[test]
    fn plural_rules_match_the_c_sharp() {
        assert_eq!(pluralize("sword"), "swords");
        assert_eq!(pluralize("torch"), "torches");
        assert_eq!(pluralize("box"), "boxes");
        assert_eq!(pluralize("moth"), "moth");
        // The intentional quirk, per the packet capture the C# cites.
        assert_eq!(pluralize("Sarcophagus"), "Sarcophaguss");
    }

    #[test]
    fn teens_take_th_not_st_nd_rd() {
        assert_eq!(ordinal(11).unwrap(), "11th");
        assert_eq!(ordinal(12).unwrap(), "12th");
        assert_eq!(ordinal(13).unwrap(), "13th");
        assert_eq!(ordinal(111).unwrap(), "111th");
    }

    #[test]
    fn ordinary_ordinals() {
        assert_eq!(ordinal(1).unwrap(), "1st");
        assert_eq!(ordinal(22).unwrap(), "22nd");
        assert_eq!(ordinal(103).unwrap(), "103rd");
        assert_eq!(ordinal(0).unwrap(), "0th");
    }

    #[test]
    fn negatives_return_none_instead_of_throwing() {
        assert!(ordinal(-1).is_none());
    }
}
