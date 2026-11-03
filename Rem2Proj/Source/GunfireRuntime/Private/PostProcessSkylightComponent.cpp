#include "PostProcessSkylightComponent.h"

UPostProcessSkylightComponent::UPostProcessSkylightComponent(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->SkyLightActorCls = NULL;
    this->SkyLight = NULL;
}

USkyLightComponent* UPostProcessSkylightComponent::GetCurrentSkylight() {
    return NULL;
}


void UPostProcessSkylightComponent::OnRegister()
{

}

void UPostProcessSkylightComponent::OnUnregister()
{

}

void UPostProcessSkylightComponent::Serialize(FArchive& Ar)
{

}