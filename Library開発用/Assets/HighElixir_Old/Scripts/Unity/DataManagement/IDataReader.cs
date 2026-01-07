using System;
using System.Threading.Tasks;

namespace HighElixir.DataManagements.DataReader
{
    public interface IDataReader
    {
        /// <exception cref="ArgumentNullException">ArgumentNullException</exception>
        Task<T> ReadDataAsync<T>(string filePath, IProgress<float> progress);
    }
}