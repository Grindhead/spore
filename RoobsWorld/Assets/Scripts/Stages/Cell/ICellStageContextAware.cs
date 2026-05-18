namespace RoobsWorld.Stages.Cell
{
    public interface ICellStageContextAware
    {
        void BindCellStageContext(CellStage stage, FoodManager foodManager);
    }
}
