using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class BlockBuildSound : MonoBehaviour
{
    [Header("Block setzen")]
    [Tooltip("Sounds fuer das Setzen eines Blocks. Bei mehreren Clips wird zufaellig gewaehlt.")]
    public AudioClip[] buildSounds;

    [Range(0f, 1f)]
    public float buildVolume = 0.7f;

    [Header("Block abbauen")]
    [Tooltip("Sounds fuer das Abbauen eines Blocks. Bei mehreren Clips wird zufaellig gewaehlt.")]
    public AudioClip[] removeSounds;

    [Range(0f, 1f)]
    public float removeVolume = 0.7f;

    [Header("Variation")]
    public bool randomPitch = true;

    [Range(0.5f, 1.5f)]
    public float pitchMin = 0.92f;

    [Range(0.5f, 1.5f)]
    public float pitchMax = 1.08f;

    [Tooltip("Erlaubt, dass mehrere kurze Block-Sounds gleichzeitig ausklingen.")]
    public bool allowOverlap = true;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
        ValidatePitchRange();
    }

    private void OnValidate()
    {
        ValidatePitchRange();
    }

    private void ValidatePitchRange()
    {
        pitchMin = Mathf.Clamp(pitchMin, 0.5f, 1.5f);
        pitchMax = Mathf.Clamp(pitchMax, 0.5f, 1.5f);

        if (pitchMin > pitchMax)
        {
            float temp = pitchMin;
            pitchMin = pitchMax;
            pitchMax = temp;
        }
    }

    public void PlayBuildSound()
    {
        PlayRandomSound(buildSounds, buildVolume);
    }

    public void PlayRemoveSound()
    {
        PlayRandomSound(removeSounds, removeVolume);
    }

    private void PlayRandomSound(AudioClip[] sounds, float volume)
    {
        if (audioSource == null || sounds == null || sounds.Length == 0)
            return;

        int validCount = 0;
        for (int i = 0; i < sounds.Length; i++)
            if (sounds[i] != null) validCount++;

        if (validCount == 0)
            return;

        int wanted = Random.Range(0, validCount);
        AudioClip selectedClip = null;

        for (int i = 0; i < sounds.Length; i++)
        {
            if (sounds[i] == null) continue;
            if (wanted == 0)
            {
                selectedClip = sounds[i];
                break;
            }
            wanted--;
        }

        if (selectedClip == null)
            return;

        audioSource.pitch = randomPitch ? Random.Range(pitchMin, pitchMax) : 1f;

        if (!allowOverlap)
            audioSource.Stop();

        audioSource.PlayOneShot(selectedClip, Mathf.Clamp01(volume));
    }
}
