using ContainerDelivery.Core.Entities;
using ContainerDelivery.Core.Interfaces;

namespace ContainerDelivery.Core.Specifications;

public class CompanySettingsSpec : BaseSpecification<CompanySettings>
{
    public CompanySettingsSpec()
    {
        Criteria = cs => cs.IsActive;
    }
}

public class CompanySettingsByIdSpec : BaseSpecification<CompanySettings>
{
    public CompanySettingsByIdSpec(int id)
    {
        Criteria = cs => cs.Id == id;
    }
}