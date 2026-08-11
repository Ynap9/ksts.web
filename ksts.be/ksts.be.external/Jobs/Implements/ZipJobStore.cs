using ksts.be.external.Jobs.Dtos;
using ksts.be.external.Jobs.Interfaces;
using System.Collections.Concurrent;

namespace ksts.be.external.Jobs.Implements
{
    /// <summary>
    /// Kho trạng thái lô dựng file nén, giữ trong bộ nhớ tiến trình.
    /// </summary>
    public class ZipJobStore : IZipJobStore
    {
        /// <summary>Lô sống 6 tiếng kể từ lúc mở — đủ cho lô 5000 file chạy xong rồi tải về thong thả.</summary>
        private const int GioSong = 6;

        private readonly ConcurrentDictionary<string, ZipJobDto> _jobs = new();

        /// <inheritdoc/>
        public ZipJobDto Tao(int tongSo)
        {
            var job = new ZipJobDto
            {
                JobId = Guid.NewGuid().ToString("N"),
                TaiToken = Guid.NewGuid().ToString("N"),
                TongSo = tongSo,
                HetHanUtc = DateTime.UtcNow.AddHours(GioSong)
            };

            _jobs[job.JobId] = job;
            return job;
        }

        /// <inheritdoc/>
        public ZipJobDto? Lay(string jobId) => _jobs.TryGetValue(jobId, out var job) ? job : null;

        /// <inheritdoc/>
        public void CapNhat(string jobId, Action<ZipJobDto> thayDoi)
        {
            if (!_jobs.TryGetValue(jobId, out var job))
            {
                return;
            }

            lock (job)
            {
                thayDoi(job);
            }
        }

        /// <inheritdoc/>
        public void DonHetHan()
        {
            foreach (var item in _jobs.Where(x => x.Value.HetHanUtc < DateTime.UtcNow).ToList())
            {
                if (!_jobs.TryRemove(item.Key, out var job))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(job.DuongDanZip) && File.Exists(job.DuongDanZip))
                {
                    try
                    {
                        File.Delete(job.DuongDanZip);
                    }
                    catch (IOException)
                    {
                        // File đang được tải dở: để lần dọn sau xoá, không làm hỏng cả vòng dọn.
                    }
                }
            }
        }
    }
}
