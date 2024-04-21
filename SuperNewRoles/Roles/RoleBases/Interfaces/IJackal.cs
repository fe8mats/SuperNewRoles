using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperNewRoles.Roles.RoleBases.Interfaces;
public interface IJackal : IKiller, IVentAvailable
{
    public bool CanSidekick { get; }
    public void SetAmSidekicked();
}