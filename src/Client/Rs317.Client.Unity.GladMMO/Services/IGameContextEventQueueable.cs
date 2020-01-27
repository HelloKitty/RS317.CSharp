using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Rs317.GladMMO
{
	public interface IGameContextEventQueueable
	{
		void Enqueue(Action actionEvent);

		void EnqueueAsync(Func<Task> actionEventAsync);
	}
}
