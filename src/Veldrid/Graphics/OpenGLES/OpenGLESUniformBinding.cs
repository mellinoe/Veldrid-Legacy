namespace Veldrid.Graphics.OpenGLES
{
    public class OpenGLESUniformBinding
    {
        public int ProgramID { get; }
        public int BlockLocation { get; } = -1;
        public int DataSizeInBytes { get; } = -1;
        public OpenGLESUniformStorageAdapter StorageAdapter { get; }

        public OpenGLESUniformBinding(int programID, int blockLocation, int dataSizeInBytes)
        {
            ProgramID = programID;
            BlockLocation = blockLocation;
            DataSizeInBytes = dataSizeInBytes;
        }

        public OpenGLESUniformBinding(int programID, OpenGLESUniformStorageAdapter storageAdapter)
        {
            ProgramID = programID;
            StorageAdapter = storageAdapter;
        }
    }
}
