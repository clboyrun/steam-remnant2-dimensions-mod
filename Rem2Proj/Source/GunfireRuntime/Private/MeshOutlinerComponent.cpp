#include "MeshOutlinerComponent.h"

UMeshOutlinerComponent::UMeshOutlinerComponent(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->bStartOutlined = true;
    this->OutlineStencilValue = 4;
    this->OutlineExclusionTag = TEXT("NoOutline");
}

void UMeshOutlinerComponent::SetOwnerOutlined(bool bEnable) {
}

bool UMeshOutlinerComponent::IsCurrentlyOutlined() const {
    return false;
}


