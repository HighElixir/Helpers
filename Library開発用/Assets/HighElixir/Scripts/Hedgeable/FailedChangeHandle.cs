namespace HighElixir.Hedgeable
{
    public enum FailedChangeHandle
    {
        Revert, // 変更前の値に戻す
        Clamp,  // 有効な範囲にクランプする
        ReturnToZero // 0に戻す (ゼロが範囲内にない場合、最も小さい端にクランプする)
    }
}