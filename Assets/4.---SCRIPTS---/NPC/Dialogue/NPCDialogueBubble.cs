using UnityEngine;
using TMPro;
using System.Collections;

public class NPCDialogueBubble : MonoBehaviour
{
    public float lifetime = 3f;
    public Vector3 offset = new Vector3(0, 1.4f, 0);

    public TMP_FontAsset runeFont;
    public TMP_FontAsset englishFont;

    Transform target;
    TextMeshPro text;
    float timer;

    void Awake()
    {
        text = GetComponent<TextMeshPro>();

        if (text == null)
            Debug.LogError("TextMeshPro component missing!");
    }

    public void Initialize(Transform followTarget, DialogueLine line)
{
    StopAllCoroutines();

    timer = 0f;
    target = followTarget;

    // Säkerställ att vi har text-referensen
    if (text == null)
        text = GetComponent<TextMeshPro>();

    // Rensa eventuell gammal text
    text.text = "";
    text.ForceMeshUpdate();

    StartCoroutine(PlayRuneMorph(line));
}

    void Update()
    {
        if (target != null)
            transform.position = target.position + offset;

        timer += Time.deltaTime;

        // Fade out sista 0.3 sekunderna
        if (timer >= lifetime - 0.3f)
        {
            float t = (timer - (lifetime - 0.3f)) / 0.3f;
            text.alpha = 1f - t;
        }

        if (timer >= lifetime)
            Destroy(gameObject);
    }

    IEnumerator PlayRuneMorph(DialogueLine line)
    {
        string runeText = line.swedish;
        string englishText = line.english;

        text.font = runeFont;
        // Ensure TMP won't truncate text due to overflow
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        text.text = runeText;
        text.alpha = 1f;
        text.ForceMeshUpdate();

        yield return StartCoroutine(Fade(0f, 1f, 0.25f));

        yield return new WaitForSeconds(0.8f);

        yield return StartCoroutine(RevealTranslation(runeText, englishText));
    }

    IEnumerator RevealTranslation(string runeText, string englishText)
    {
        float duration = 0.6f;
        float elapsed = 0f;

        text.ForceMeshUpdate();
        TMP_TextInfo textInfo = text.textInfo;

        int charCount = textInfo.characterCount;

        float revealWidth = 1.2f; // hur mjuk övergången är

        while (elapsed < duration)
        {
            float t = elapsed / duration;

            for (int i = 0; i < charCount; i++)
            {
                if (!textInfo.characterInfo[i].isVisible)
                    continue;

                int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
                int vertexIndex = textInfo.characterInfo[i].vertexIndex;
                Color32[] colors = textInfo.meshInfo[materialIndex].colors32;

                float charPosition = (float)i / charCount;

                float fade = Mathf.Clamp01((t - charPosition) * revealWidth);

                byte alpha = (byte)Mathf.Lerp(255, 0, fade);

                for (int j = 0; j < 4; j++)
                    colors[vertexIndex + j].a = alpha;
            }

            text.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // När runorna är borta – byt text
        text.font = englishFont;
        // ensure overflow and wrapping are set so full english line is rendered
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        text.text = englishText;
        text.ForceMeshUpdate();
        text.UpdateVertexData(TMP_VertexDataUpdateFlags.All);

        // Fade in engelska från vänster
        elapsed = 0f;
        textInfo = text.textInfo;
        charCount = textInfo.characterCount;

        while (elapsed < duration)
        {
            float t = elapsed / duration;

            for (int i = 0; i < charCount; i++)
            {
                if (!textInfo.characterInfo[i].isVisible)
                    continue;

                int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
                int vertexIndex = textInfo.characterInfo[i].vertexIndex;
                Color32[] colors = textInfo.meshInfo[materialIndex].colors32;

                float charPosition = (float)i / charCount;

                float fade = Mathf.Clamp01((t - charPosition) * revealWidth);

                byte alpha = (byte)Mathf.Lerp(0, 255, fade);

                for (int j = 0; j < 4; j++)
                    colors[vertexIndex + j].a = alpha;
            }

            text.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

            elapsed += Time.deltaTime;
            yield return null;
            text.alpha = 1f;
        }
        text.alpha = 1f;

        // Ensure final vertex colors are fully visible — force TMP to rebuild mesh so no characters remain with alpha 0
        text.ForceMeshUpdate();
        text.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }

    IEnumerator PopCharacter(int index)
    {
        text.ForceMeshUpdate();
        TMP_TextInfo textInfo = text.textInfo;

        int materialIndex = textInfo.characterInfo[index].materialReferenceIndex;
        int vertexIndex = textInfo.characterInfo[index].vertexIndex;

        Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

        Vector3[] originalVertices = new Vector3[4];

        for (int i = 0; i < 4; i++)
            originalVertices[i] = vertices[vertexIndex + i];

        float duration = 0.20f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;

            float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.4f;

            Vector3 center = (originalVertices[0] + originalVertices[2]) / 2;

            for (int i = 0; i < 4; i++)
            {
                Vector3 offset = originalVertices[i] - center;
                vertices[vertexIndex + i] = center + offset * scale;
            }

            text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);

            elapsed += Time.deltaTime;
            yield return null;
        }

        for (int i = 0; i < 4; i++)
            vertices[vertexIndex + i] = originalVertices[i];

        text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float alpha = Mathf.Lerp(from, to, t);
            text.alpha = alpha;

            elapsed += Time.deltaTime;
            yield return null;
        }

        text.alpha = to;
    }
}