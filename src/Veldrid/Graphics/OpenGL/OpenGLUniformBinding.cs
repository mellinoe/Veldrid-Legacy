namespace Veldrid.Graphics.OpenGL
{
    public class OpenGLUniformBinding
    {
        public int ProgramID { get; }
        public int BlockLocation { get; } = -1;
        public int DataSizeInBytes { get; } = -1;
        public OpenGLUniformStorageAdapter StorageAdapter { get; }

        public OpenGLUniformBinding(int programID, int blockLocation, int dataSizeInBytes)
        {
            ProgramID = programID;
            BlockLocation = blockLocation;
            DataSizeInBytes = dataSizeInBytes;
        }

        public OpenGLUniformBinding(int programID, OpenGLUniformStorageAdapter storageAdapter)
        {
            ProgramID = programID;
            StorageAdapter = storageAdapter;
        }
    }
}
