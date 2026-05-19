// import { useGetLocationSaveTemplatedLocationFromKey } from '@/api/config'
import {
  useCreateLocation,
  useLatestVersionOfAllLocations,
} from '@/features/locations/api'
import { useGetJurisdiction } from '@/features/jurisdictions/api/jurisdictionApi'
import { Location, LocationExpanded } from '@/features/locations/types'
import { useGetRegion } from '@/features/region/api/regionApi'
import { useEnv } from '@/hooks/useEnv'
import { queryClient } from '@/lib/react-query'
import { zodResolver } from '@hookform/resolvers/zod'
import CheckCircleOutlineOutlinedIcon from '@mui/icons-material/CheckCircleOutlineOutlined'
import ErrorOutlineIcon from '@mui/icons-material/ErrorOutline'
import { LoadingButton } from '@mui/lab'
import {
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  InputAdornment,
  TextField,
} from '@mui/material'
import { Controller, useForm } from 'react-hook-form'
import { z } from 'zod'

interface NewLocationModalProps {
  closeModal: () => void
  setLocation: (location: Location) => void
  onCreatedFromTemplate: () => void
}

// Schemas
// For no template: Only locationIdentifier is required
const noTemplateSchema = z.object({
  locationIdentifier: z
    .string()
    .min(1, { message: 'Location Identifier is required.' })
    .max(10, {
      message: 'Location Identifier must be 10 characters or fewer.',
    }),
})

// For template: locationIdentifier + latitude + longitude + devices (with ipaddress)
const templateSchema = z.object({
  locationIdentifier: z
    .string()
    .min(1, { message: 'Location Identifier is required.' })
    .max(10, {
      message: 'Location Identifier must be 10 characters or fewer.',
    }),
  primaryName: z.string().optional(),
  secondaryName: z.string().optional(),
  latitude: z
    .union([z.string(), z.number()])
    .transform((val) => (val === '' ? NaN : Number(val)))
    .refine((val) => !isNaN(val), {
      message: 'Latitude is required when copying from template',
    }),
  longitude: z
    .union([z.string(), z.number()])
    .transform((val) => (val === '' ? NaN : Number(val)))
    .refine((val) => !isNaN(val), {
      message: 'Longitude is required when copying from template',
    }),
  devices: z
    .array(
      z.object({
        ipaddress: z.string().min(1, { message: 'IP Address is required.' }),
      })
    )
    .min(1, {
      message: 'At least one device is required when copying from template',
    }),
})

const NewLocationModal = ({
  closeModal,
  setLocation,
  onCreatedFromTemplate,
}: NewLocationModalProps) => {
  // const [selectedLocation, setSelectedLocation] = useState<Location | null>(
  //   null
  // )
  // const [copyLocationFromTemplate, setCopyLocationFromTemplate] =
  //   useState<boolean>(false)

  // const locationHandler = useLocationConfigHandler({
  //   location: selectedLocation as Location,
  // })

  const { mutate: createLocation } = useCreateLocation()
  const { data: allLocationsData } = useLatestVersionOfAllLocations()
  const { data: jurisdictionData } = useGetJurisdiction()
  const { data: regionData } = useGetRegion()
  const { data: envData } = useEnv()

  const allLocations = allLocationsData?.value || []
  const jurisdictions = jurisdictionData?.value || []
  const regions = regionData?.value || []
  const defaultJurisdictionId = jurisdictions[0]?.id
  const defaultRegionId = regions[0]?.id
  const defaultLatitude = Number(envData?.MAP_DEFAULT_LATITUDE ?? 40.758701)
  const defaultLongitude = Number(envData?.MAP_DEFAULT_LONGITUDE ?? -111.876183)

  const {
    control,
    handleSubmit,
    formState: { errors, isSubmitting },
    watch,
  } = useForm<LocationExpanded>({
    resolver: zodResolver(noTemplateSchema),
    defaultValues: {
      locationIdentifier: '',
      primaryName: '',
      secondaryName: '',
      latitude: undefined,
      longitude: undefined,
      devices: [],
    },
  })

  const locationIdentifier = watch('locationIdentifier')

  const locationIsUnique = !allLocations.find(
    (loc: { locationIdentifier: string }) =>
      loc.locationIdentifier === locationIdentifier
  )
  const locationIsLessThan10Characters = (locationIdentifier || '').length <= 10

  const onSubmit = async (data: LocationExpanded) => {
    // const devices = locationHandler?.expandedLocation?.devices || []
    // const transformedDevices = devices.map((device, index) => {
    //   const { id, locationId, ...rest } = device
    //   return {
    //     ...rest,
    //     ipaddress: data.devices ? data.devices[index].ipaddress : '',
    //   }
    // })

    // if (copyLocationFromTemplate && selectedLocation) {
    //   const templateData = {
    //     locationIdentifier: data.locationIdentifier,
    //     primaryName: data.primaryName || '',
    //     secondaryName: data.secondaryName || '',
    //     latitude: data.latitude || null,
    //     longitude: data.longitude || null,
    //     devices: transformedDevices,
    //   }

    //   await createFromTemplate(
    //     {
    //       key: parseInt(selectedLocation.id),
    //       data: templateData,
    //     },
    //     {
    //       onSuccess: (createdData) => {
    //         setLocation(createdData as unknown as Location)

    //         onCreatedFromTemplate()
    //       },
    //       onSettled: closeModal,
    //     }
    //   )
    // } else {
    // If not copying template, we just need locationIdentifier.
    const defaultValues = {
      locationIdentifier: data.locationIdentifier,
      note: '',
      start: new Date().toISOString(),
      primaryName: '',
      secondaryName: '',
      latitude: Number.isFinite(defaultLatitude) ? defaultLatitude : 40.758701,
      longitude: Number.isFinite(defaultLongitude)
        ? defaultLongitude
        : -111.876183,
      pedsAre1to1: false,
      locationTypeId: 1,
      chartEnabled: true,
      regionId: defaultRegionId,
      jurisdictionId: defaultJurisdictionId,
      versionAction: 'Initial',
    }

    createLocation(defaultValues, {
      onSuccess: (createdData) => {
        const createdLocation = (createdData as any).data as Location

        queryClient.setQueryData(['locations'], (previous: any) => {
          if (!previous?.value) return previous

          const alreadyExists = previous.value.some(
            (loc: Location) => loc.id === createdLocation.id
          )

          if (alreadyExists) return previous

          return {
            ...previous,
            value: [createdLocation, ...previous.value],
          }
        })

        queryClient.invalidateQueries(['locations'])
        setLocation(createdLocation)
      },
      onSettled: closeModal,
    })
  }
  // }

  const errorMessage = () => {
    if (!defaultJurisdictionId) {
      return 'Create at least one jurisdiction in Admin > Jurisdictions before creating a location.'
    }
    if (!defaultRegionId) {
      return 'Create at least one region in Admin > Regions before creating a location.'
    }
    if (errors.locationIdentifier) {
      return errors.locationIdentifier.message
    }
    if (!locationIsLessThan10Characters) {
      return 'Location Identifier must be 10 characters or fewer.'
    }
    if (!locationIsUnique) {
      return 'Location Identifier already exists.'
    }
    return ''
  }

  return (
    <Dialog
      open={true}
      onClose={closeModal}
      PaperProps={{
        sx: {
          padding: 2,
          minWidth: 400,
          maxWidth: 480,
        },
      }}
    >
      <DialogTitle variant="h4" sx={{ fontWeight: 'bold' }}>
        New Location
      </DialogTitle>
      <form onSubmit={handleSubmit(onSubmit)}>
        <DialogContent>
          <Box sx={{ width: '60%', minWidth: '400px' }}>
            <Controller
              name="locationIdentifier"
              control={control}
              render={({ field }) => (
                <TextField
                  {...field}
                  fullWidth
                  autoComplete="off"
                  error={
                    !!errors.locationIdentifier ||
                    !locationIsUnique ||
                    !locationIsLessThan10Characters
                  }
                  color="success"
                  InputProps={{
                    endAdornment: locationIdentifier ? (
                      <InputAdornment position="end">
                        {locationIsUnique && locationIsLessThan10Characters ? (
                          <CheckCircleOutlineOutlinedIcon color="success" />
                        ) : (
                          <ErrorOutlineIcon color="error" />
                        )}
                      </InputAdornment>
                    ) : null,
                  }}
                  helperText={errorMessage()}
                  label="Location Identifier"
                  sx={{ marginBottom: 1 }}
                />
              )}
            />
          </Box>
        </DialogContent>
        <DialogActions sx={{ p: 2 }}>
          <Button
            onClick={closeModal}
            variant="outlined"
            disabled={isSubmitting}
          >
            Cancel
          </Button>
          <LoadingButton
            variant="contained"
            color="success"
            type="submit"
            loading={isSubmitting}
            disabled={
              !locationIsUnique ||
              !!errors.locationIdentifier ||
              !locationIdentifier ||
              !locationIsLessThan10Characters ||
              !defaultJurisdictionId ||
              !defaultRegionId
            }
          >
            Create Location
          </LoadingButton>
        </DialogActions>
      </form>
    </Dialog>
  )
}

export default NewLocationModal
