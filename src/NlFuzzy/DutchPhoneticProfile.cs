namespace NlFuzzy;

/// <summary>Determines how aggressively phonetically similar sounds are merged.</summary>
public enum DutchPhoneticProfile
{
    /// <summary>Keeps voicing distinctions except for word-final devoicing. Higher precision.</summary>
    Strict = 0,

    /// <summary>Merges b/p, d/t, v/f/w, z/s and g/ch. Higher recall. Recommended for names.</summary>
    Tolerant = 1
}
