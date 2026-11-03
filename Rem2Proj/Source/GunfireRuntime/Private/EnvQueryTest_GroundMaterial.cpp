#include "EnvQueryTest_GroundMaterial.h"

UEnvQueryTest_GroundMaterial::UEnvQueryTest_GroundMaterial() {
    this->TestPurpose = EEnvTestPurpose::Filter;
    this->FilterType = EEnvTestFilterType::Match;
    this->ScoringEquation = EEnvTestScoreEquation::Constant;
    this->TraceChannel = ECC_WorldStatic;
    this->TraceComplex = true;
    this->TraceOffset = 50.00f;
    this->TraceDistance = -300.00f;
    this->MinDistance = -1.00f;
    this->MaxDistance = -2.00f;
    this->SampleCount = 0;
    this->SampleRadius = 50.00f;
    this->SampleOffset = 10.00f;
    this->SampleDistance = 30.00f;
}


