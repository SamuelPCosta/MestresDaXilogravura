using UnityEngine;

namespace Xilo.Core
{
    [CreateAssetMenu(menuName = "Xilo/Step Definition", fileName = "Step_")]
    public sealed class StepDefinition : ScriptableObject
    {
        [SerializeField] StepId id;
        [SerializeField] string title;
        [SerializeField, TextArea(2, 5)] string instruction;

        [Header("Interaction")]
        [SerializeField] ToolId toolId = ToolId.None;
        [SerializeField] SurfaceId surface = SurfaceId.None;
        [SerializeField] string maskProperty;

        [Header("Coverage")]
        [SerializeField] CoverageTarget coverageTarget = CoverageTarget.FullSurface;
        [SerializeField, Range(0f, 1f), Tooltip("Fraction of the possible area that must be covered to unlock Next.")]
        float minCoverage = 0.1f;

        [Header("Audio")]
        [SerializeField] AudioClip instructionClip;
        [SerializeField] AudioClip toolLoopClip;

        public StepId Id => id;
        public string Title => title;
        public string Instruction => instruction;
        public ToolId ToolId => toolId;
        public SurfaceId Surface => surface;
        public string MaskProperty => maskProperty;
        public CoverageTarget CoverageTarget => coverageTarget;
        public float MinCoverage => minCoverage;
        public AudioClip InstructionClip => instructionClip;
        public AudioClip ToolLoopClip => toolLoopClip;
    }
}
