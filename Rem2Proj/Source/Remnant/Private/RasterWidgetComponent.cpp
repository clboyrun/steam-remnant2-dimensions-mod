#include "RasterWidgetComponent.h"

URasterWidgetComponent::URasterWidgetComponent(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->MaterialOverride = NULL;
}

UTexture* URasterWidgetComponent::GetWidgetTexture() {
    return NULL;
}

void URasterWidgetComponent::ForceRedraw() {
}

UMaterialInstanceDynamic* URasterWidgetComponent::CreateWidgetMaterial(UPrimitiveComponent* Primitive, UMaterialInterface* SourceMaterial, FName MaterialTextureName, int32 ElementIndex) {
    return NULL;
}


