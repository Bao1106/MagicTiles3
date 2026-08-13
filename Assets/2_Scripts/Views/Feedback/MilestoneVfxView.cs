using UnityEngine;

/// <summary>Plays the milestone VFX tier that matches the combo just reached.</summary>
public class MilestoneVfxView : MonoBehaviour
{
    [Tooltip("One root per entry in ScoreModel.Milestones, same order.")]
    [SerializeField] private GameObject[] tiers;
    [SerializeField] private GameObject transition;

    [Tooltip("Snap to this object's Y before playing. NoteController places the hit line " +
             "from the camera at runtime, so its height is not known in the scene.")]
    [SerializeField] private Transform anchor;

    private ParticleSystem[][] tierSystems;
    private ParticleSystem[] transitionSystems;

    private void Awake()
    {
        tierSystems = new ParticleSystem[tiers.Length][];
        for (int i = 0; i < tiers.Length; i++)
            tierSystems[i] = tiers[i].GetComponentsInChildren<ParticleSystem>(true);

        transitionSystems = transition.GetComponentsInChildren<ParticleSystem>(true);
    }

    private void OnEnable() => GameEvents.ComboChanged += HandleComboChanged;
    private void OnDisable() => GameEvents.ComboChanged -= HandleComboChanged;

    private void HandleComboChanged(int combo)
    {
        int i = System.Array.IndexOf(ScoreModel.Milestones, combo);
        if (i < 0 || i >= tierSystems.Length) return;

        if (anchor != null)
        {
            Vector3 p = transform.position;
            transform.position = new Vector3(p.x, anchor.position.y, p.z);
        }

        Play(transitionSystems);
        Play(tierSystems[i]);
    }

    // withChildren: false — the array already holds every nested system.
    private static void Play(ParticleSystem[] systems)
    {
        for (int i = 0; i < systems.Length; i++) systems[i].Play(false);
    }
}
