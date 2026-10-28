namespace Xilo.Core
{
    public enum StepId
    {
        ArtSelection,
        Sketching,
        Carving,
        Sanding,
        InkPouring,
        RollerLoading,
        MatrixInking,
        PaperPlacement,
        Pressing,
        Reveal
    }

    public enum ToolId
    {
        None,
        Pencil,
        Gouge,
        Sandpaper,
        InkPot,
        Roller,
        Paper,
        Baren
    }

    public enum SurfaceId
    {
        None,
        Matrix,
        Glass,
        Paper
    }

    public enum CoverageTarget
    {
        FullSurface,
        ArtDark,
        ArtLight,
        Uncarved,
        Inked
    }
}
