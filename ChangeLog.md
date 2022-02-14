
n.n.n / 2022-02-14
==================

  * AssemblyVersion
  * AF:csproj
  * csproj: added NIGHTBUILD flag to debug configuration
  * KSP: reference changed 1.10.1 => 1.11.1

v1.2.1.1 / 2022-02-14
==================

  * AssemblyVersion
  * AF:csproj
  * csproj: added NIGHTBUILD flag to debug configuration
  * KSP: reference changed 1.10.1 => 1.11.1

v1.2.1 / 2020-07-05
===================

  * KSP refs: 1.10.0
  * AssemblyVersion
  * Using Part.UpdateCoMOffset to set CoM in MultiGeometryAnimator
  * Changed references to KSP-1.9.1

v1.2.0 / 2019-12-22
===================

  * AssemblyVersion: 1.2.0
  * Fixed DEBUG code
  * Moved Forward/ReverseSpeed rescaling from Hangar to base updater
  * In editor play animations in fixed time
  * Calculating hasSound and hasEmitter in OnStart once to avoid expencive null check
  * AF+Refactoring
  * Renamed aname to clipName for clarity
  * Renamed mult parameter to multiplier for clarity
  * Renamed ntime to n_time for clarity
  * Removed redundant override of FixedUpdate in MultiLights
  * Changed enable/disable light labels in PAW
  * Log found animations and lights in DEBUG
  * Corrected typos
  * Deprecated AnimatedGroundAnchor
  * Added AT_Utils.IAnimator interface

v1.1.1 / 2019-11-30
===================

  * ToggleAction of multianimators syncs with its action goup if it's set
  * AssemblyVersion: 1.1.1
  * AF
  * In MultiGeometryAnimator made drag cube names configurable
  * Set new UnityEngine.Module dlls as non-private: don't copy them to the output
  * Added required Unity-2019 Module dlls
  * Changed target framework to .NET-4.5
  * REFS KSP-1.8.1
  * REFS KSP-1.7.3
  * REFS: KSP-1.7.0
  * Changed references to KSP-1.6.0
  * Changed references to KSP-1.4.5
  * TABS to SPACES
  * Animation speed depends on time warp rate.
  * Added animated anchor module. Just a normal ATGroundAnchor with animation.
  * Changed references to KSP-1.4.3
  * Converted tabs to spaces.
  * Changed references to KSP-1.4.1
  * Using List initializer.
  * Changed references to KSP-1.3.1
  * Changed version to 1.1.0.2
  * Changed references to KSP-1.3
  * Changed version to 1.1.0.1
  * Added nightbuilds.
  * All string KSPFields defaults to "". Added warning on Duration == 0.
  * Moved to fully-manual versioning to avoid unpredicted revision number changes.
  * Updated references to 1.2.2.
  * Using SerializableFiledsPartModule and KSPField for the CoMCurve persistence.
  * Using KSPField for CoMCurve, as any IConfigModule classes are Saved/Loaded.
  * Added MultiAnimator.MaxPitch option.
  * Changed references to 1.2.1
  * Updated to KSP-1.2 API.
  * Added GeometryAnimatorUpdater that scales CoMCurve.
  * Added MultiAnimator.ntime property.
  * Untangled seek/set_progress framework; added CoMCurve VectorCurve to animate part.CoMOffset
  * Moved dll-s into Plugins subfolder.
  * Fixed debug logging.
  * Fixed output directory.
  * Moved under the AT_Utils.
  * Added link to AT_Utils to readme.
  * Inital commit.
  * Update README.md
  * Initial commit
